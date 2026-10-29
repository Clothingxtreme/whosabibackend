using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net.Http.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using WhoSabi.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;

namespace WhoSabi.Api.Platform;

public static class MediaEndpoints
{
    public sealed record MediaControlRequest(Guid UserId, bool CanPublish = false, bool Remove = false);
    public static void MapMedia(this WebApplication app)
    {
        app.MapPost("/api/rooms/{roomId}/media/token", async (string roomId, ClaimsPrincipal user, WhoSabiDbContext db, IConfiguration config, CancellationToken ct) => {
            var uid = user.UserId(); await ApiSupport.Member(db, roomId, uid, ct);
            var secret = config["LiveKit:ApiSecret"]; var key = config["LiveKit:ApiKey"]; var url = config["LiveKit:Url"];
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(key) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "wss")
                throw new ApiFault(503, "MEDIA_NOT_CONFIGURED", "A LiveKit service and API credentials are required.");
            var room = await db.Rooms.SingleAsync(x => x.Id == roomId, ct);
            if (room.Status != WhoSabi.Domain.Rooms.RoomStatus.Live) throw new ApiFault(409, "ROOM_NOT_LIVE", "Room is closed.");
            var isHost = room.HostUserId == uid;
            var participant = await db.RoomParticipants.SingleAsync(x => x.RoomId == roomId && x.UserId == uid, ct);
            var now = DateTimeOffset.UtcNow; var expires = now.AddMinutes(5);
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
            var payload = new JwtPayload {
                ["iss"] = key, ["sub"] = uid.ToString(), ["nbf"] = now.ToUnixTimeSeconds(), ["exp"] = expires.ToUnixTimeSeconds(),
                ["video"] = new Dictionary<string, object> { ["room"] = roomId, ["roomJoin"] = true, ["roomAdmin"] = isHost,
                    ["canPublish"] = isHost || participant.CanPublishMedia, ["canSubscribe"] = true, ["canPublishData"] = false }
            };
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(credentials), payload));
            return Results.Ok(new { url, token, expiresAt = expires, canPublish = isHost || participant.CanPublishMedia });
        }).RequireAuthorization();
        app.MapPost("/api/rooms/{roomId}/media/participants", async (string roomId, MediaControlRequest input, ClaimsPrincipal user, WhoSabiDbContext db, IConfiguration config, IHttpClientFactory clients, RoomConnections connections, IHubContext<RoomHub> hub, CancellationToken ct) => {
            await ApiSupport.Host(db, roomId, user.UserId(), ct);
            if (input.UserId == user.UserId()) throw new ApiFault(400, "INVALID_TARGET", "Select a participant.");
            var target = await db.RoomParticipants.SingleOrDefaultAsync(x => x.RoomId == roomId && x.UserId == input.UserId && x.Status == WhoSabi.Domain.Rooms.RoomParticipantStatus.Active, ct)
                ?? throw new ApiFault(404, "PARTICIPANT_NOT_FOUND", "Participant not found.");
            var secret = config["LiveKit:ApiSecret"]; var key = config["LiveKit:ApiKey"]; var url = config["LiveKit:Url"];
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(key) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "wss")
                throw new ApiFault(503, "MEDIA_NOT_CONFIGURED", "Configure LiveKit first.");
            var now = DateTimeOffset.UtcNow;
            var payload = new JwtPayload { ["iss"] = key, ["nbf"] = now.ToUnixTimeSeconds(), ["exp"] = now.AddMinutes(1).ToUnixTimeSeconds(), ["video"] = new Dictionary<string, object> { ["room"] = roomId, ["roomAdmin"] = true } };
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256)), payload));
            var baseUri = new UriBuilder(uri) { Scheme = "https" };
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri.Uri, "/twirp/livekit.RoomService/" + (input.Remove ? "RemoveParticipant" : "UpdateParticipant")));
            request.Headers.Authorization = new("Bearer", token);
            request.Content = input.Remove
                ? JsonContent.Create(new { room = roomId, identity = input.UserId.ToString() })
                : JsonContent.Create(new { room = roomId, identity = input.UserId.ToString(), permission = new { canPublish = input.CanPublish, canSubscribe = true, canPublishData = false } });
            using var response = await clients.CreateClient().SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) throw new ApiFault(502, "MEDIA_CONTROL_FAILED", "Media provider did not apply the change.");
            target.CanPublishMedia = input.CanPublish && !input.Remove;
            if (input.Remove) { target.IsBanned = true; target.Status = WhoSabi.Domain.Rooms.RoomParticipantStatus.Left; target.LeftAt = now; }
            await db.SaveChangesAsync(ct);
            if (input.Remove)
                foreach (var connection in connections.For(input.UserId, roomId))
                {
                    await hub.Groups.RemoveFromGroupAsync(connection, RoomHub.Group(roomId), ct);
                    connections.Remove(input.UserId, roomId, connection);
                }
            return Results.Ok(new { target.UserId, target.CanPublishMedia, target.IsBanned });
        }).RequireAuthorization();
    }
}
