using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WhoSabi.Domain.Platform;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public sealed class FirebasePush(HttpClient http, IConfiguration config)
{
    public bool Configured => !string.IsNullOrEmpty(config["Firebase:ProjectId"]) && !string.IsNullOrEmpty(config["Firebase:ClientEmail"]) && !string.IsNullOrEmpty(config["Firebase:PrivateKey"]);

    public async Task<bool> Send(PushDevice device, AppNotification notification, CancellationToken ct)
    {
        if (!Configured) return false;
        var now = DateTimeOffset.UtcNow;
        using var rsa = RSA.Create(); rsa.ImportFromPem(config["Firebase:PrivateKey"]!.Replace("\\n", "\n"));
        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        var payload = new JwtPayload {
            ["iss"] = config["Firebase:ClientEmail"], ["scope"] = "https://www.googleapis.com/auth/firebase.messaging",
            ["aud"] = "https://oauth2.googleapis.com/token", ["iat"] = now.ToUnixTimeSeconds(), ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds()
        };
        var assertion = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(credentials), payload));
        using var tokenResponse = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer", ["assertion"] = assertion }), ct);
        if (!tokenResponse.IsSuccessStatusCode) return false;
        using var json = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(config["Firebase:ProjectId"]!)}/messages:send");
        request.Headers.Authorization = new("Bearer", json.RootElement.GetProperty("access_token").GetString());
        request.Content = JsonContent.Create(new { message = new { token = device.Token, notification = new { title = notification.Title, body = notification.Body },
            data = new { notificationId = notification.Id.ToString(), link = notification.Link ?? "" }, android = new { notification = new { tag = notification.Id.ToString() } } } });
        using var response = await http.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }
}

public sealed class PushDispatcher(IServiceScopeFactory scopes, ILogger<PushDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<FirebasePush>();
                if (!sender.Configured) continue;
                var db = scope.ServiceProvider.GetRequiredService<WhoSabiDbContext>();
                var since = DateTimeOffset.UtcNow.AddDays(-7);
                var missing = await (from n in db.Set<AppNotification>() from d in db.Set<PushDevice>()
                    where n.UserId == d.UserId && n.CreatedAt >= since && n.CreatedAt >= d.UpdatedAt &&
                        !db.Set<PushDelivery>().Any(x => x.NotificationId == n.Id && x.DeviceId == d.Id)
                    orderby n.CreatedAt select new { NotificationId = n.Id, DeviceId = d.Id }).Take(100).ToArrayAsync(stoppingToken);
                db.AddRange(missing.Select(x => new PushDelivery { NotificationId = x.NotificationId, DeviceId = x.DeviceId }));
                await db.SaveChangesAsync(stoppingToken);
                await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);
                var now = DateTimeOffset.UtcNow;
                var due = await db.Set<PushDelivery>().FromSqlInterpolated($"SELECT * FROM push_deliveries WHERE \"Status\" = 'pending' AND \"NextAttemptAt\" <= {now} ORDER BY \"NextAttemptAt\" LIMIT 25 FOR UPDATE SKIP LOCKED").ToListAsync(stoppingToken);
                foreach (var delivery in due) { delivery.NextAttemptAt = now.AddMinutes(5); delivery.Attempts++; }
                await db.SaveChangesAsync(stoppingToken); await tx.CommitAsync(stoppingToken);
                foreach (var delivery in due)
                {
                    var notification = await db.Set<AppNotification>().SingleAsync(x => x.Id == delivery.NotificationId, stoppingToken);
                    var device = await db.Set<PushDevice>().SingleAsync(x => x.Id == delivery.DeviceId, stoppingToken);
                    if (device.UserId != notification.UserId) delivery.Status = "cancelled";
                    else if (await sender.Send(device, notification, stoppingToken)) { delivery.Status = "sent"; delivery.SentAt = DateTimeOffset.UtcNow; }
                    else if (delivery.Attempts >= 5) delivery.Status = "failed";
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogError(e, "Push delivery failed; pending messages will be retried."); }
        }
    }
}
