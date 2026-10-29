using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Auth;
using WhoSabi.Domain.Platform;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public sealed record ProfileRequest(string? PhoneNumber, string? AvatarUrl);
public sealed record PushRequest(string Token, string Platform);

public static class ProfileEndpoints
{
    public static void MapProfileAndNotifications(this WebApplication app)
    {
        app.MapGet("/api/me/profile", async (ClaimsPrincipal principal, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = principal.UserId();
            var user = await db.Users.AsNoTracking().Where(x => x.Id == uid).Select(x => new { x.Id, x.Username, x.Email, x.PhoneNumber, x.AvatarUrl, x.AcceptedPolicyVersion, x.PolicyAcceptedAt, x.IsTestAccount }).SingleOrDefaultAsync(ct);
            return user is null ? Results.NotFound() : Results.Ok(user);
        }).RequireAuthorization();
        app.MapPatch("/api/me", async (ProfileRequest input, ClaimsPrincipal principal, WhoSabiDbContext db, CancellationToken ct) => {
            var user = await db.Users.SingleAsync(x => x.Id == principal.UserId(), ct);
            if (input.PhoneNumber is not null)
            {
                try { user.PhoneNumber = RegistrationProfile.NormalizePhone(input.PhoneNumber); }
                catch (RegistrationInitiationException e) { throw new ApiFault(400, e.Code, e.Message); }
            }
            if (input.AvatarUrl is not null)
            {
                if (input.AvatarUrl == "") user.AvatarUrl = null;
                else if (input.AvatarUrl.Length <= 2048 && Uri.TryCreate(input.AvatarUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https") user.AvatarUrl = uri.AbsoluteUri;
                else throw new ApiFault(400, "INVALID_AVATAR", "Use an HTTPS image URL.");
            }
            user.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.Username, user.PhoneNumber, user.AvatarUrl, phoneVerified = false });
        }).RequireAuthorization();
        var notifications = app.MapGroup("/api/notifications").RequireAuthorization();
        notifications.MapGet("", async (DateTimeOffset? before, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId(); var query = db.Set<AppNotification>().AsNoTracking().Where(x => x.UserId == uid);
            if (before is not null) query = query.Where(x => x.CreatedAt < before);
            return Results.Ok(await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Take(50).ToArrayAsync(ct));
        });
        notifications.MapGet("/unread-count", async (ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId(); return Results.Ok(new { count = await db.Set<AppNotification>().CountAsync(x => x.UserId == uid && x.ReadAt == null, ct) });
        });
        notifications.MapPost("/{id:guid}/read", async (Guid id, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId();
            var count = await db.Set<AppNotification>().Where(x => x.Id == id && x.UserId == uid).ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, DateTimeOffset.UtcNow), ct);
            return count == 0 ? Results.NotFound() : Results.NoContent();
        });
        notifications.MapPost("/read-all", async (ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId(); await db.Set<AppNotification>().Where(x => x.UserId == uid && x.ReadAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, DateTimeOffset.UtcNow), ct); return Results.NoContent();
        });
        // Device registration works before Firebase credentials are installed; report delivery availability.
        app.MapPut("/api/me/push-devices", async (PushRequest input, ClaimsPrincipal user, WhoSabiDbContext db, FirebasePush push, CancellationToken ct) => {
            var token = ApiSupport.Required(input.Token, 4096, "Token");
            if (input.Platform is not ("android" or "ios" or "web")) throw new ApiFault(400, "INVALID_PLATFORM", "Use android, ios or web.");
            var device = await db.Set<PushDevice>().SingleOrDefaultAsync(x => x.Token == token, ct);
            if (device is null) { device = new PushDevice { Token = token }; db.Add(device); }
            device.UserId = user.UserId(); device.Platform = input.Platform; device.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct); return Results.Ok(new { device.Id, deliveryConfigured = push.Configured });
        }).RequireAuthorization();
        app.MapDelete("/api/me/push-devices/{id:guid}", async (Guid id, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId(); await db.Set<PushDevice>().Where(x => x.Id == id && x.UserId == uid).ExecuteDeleteAsync(ct); return Results.NoContent();
        }).RequireAuthorization();
    }
}
