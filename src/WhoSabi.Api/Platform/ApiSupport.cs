using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WhoSabi.Domain.Platform;
using WhoSabi.Domain.Rooms;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public sealed class ApiFault(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

public static class ApiSupport
{
    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : throw new ApiFault(401, "AUTH_REQUIRED", "Sign in again.");

    public static string Required(string? value, int max, string field)
    {
        var text = value?.Trim() ?? "";
        if (text.Length < 1 || text.Length > max)
            throw new ApiFault(400, "INVALID_INPUT", $"{field} is required and must be at most {max} characters.");
        return text;
    }

    public static async Task Member(WhoSabiDbContext db, string roomId, Guid userId, CancellationToken ct)
    {
        if (!await db.RoomParticipants.AnyAsync(x => x.RoomId == roomId && x.UserId == userId && x.Status == RoomParticipantStatus.Active, ct))
            throw new ApiFault(403, "ROOM_MEMBERSHIP_REQUIRED", "Join the room first.");
    }

    public static async Task Host(WhoSabiDbContext db, string roomId, Guid userId, CancellationToken ct)
    {
        if (!await db.Rooms.AnyAsync(x => x.Id == roomId && x.HostUserId == userId, ct))
            throw new ApiFault(403, "HOST_REQUIRED", "Only the room host can do this.");
    }

    public static void UseApiErrors(this WebApplication app) => app.Use(async (ctx, next) =>
    {
        try { await next(); }
        catch (ApiFault e) when (!ctx.Response.HasStarted)
        { ctx.Response.StatusCode = e.Status; await ctx.Response.WriteAsJsonAsync(new { code = e.Code, message = e.Message }); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" } && !ctx.Response.HasStarted)
        { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { code = "DUPLICATE_OPERATION", message = "This identifier or operation already exists. Reload before retrying." }); }
        catch (Exception e) when ((e is PostgresException { SqlState: "40001" or "40P01" } || e is DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "40P01" } }) && !ctx.Response.HasStarted)
        { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { code = "RETRY_OPERATION", message = "Concurrent update. Retry with the same idempotency key." }); }
    });
}
