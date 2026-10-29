using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WhoSabi.Application.Auth;
using WhoSabi.Domain.Platform;
using WhoSabi.Infrastructure;
using Xunit;

namespace WhoSabi.Api.Tests;

public sealed class InputTests
{
    [Theory]
    [InlineData("08012345678", "+2348012345678")]
    [InlineData("+234 801 234 5678", "+2348012345678")]
    public void PhoneNormalization(string input, string expected) => Assert.Equal(expected, RegistrationProfile.NormalizePhone(input));

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("+00000000000")]
    public void InvalidPhones(string input) => Assert.Throws<RegistrationInitiationException>(() => RegistrationProfile.NormalizePhone(input));

    [Fact]
    public void NeonUrlRetainsSecurityAndDefaultPort()
    {
        var result = new NpgsqlConnectionStringBuilder(DependencyInjection.ToNpgsqlConnectionString("postgresql://owner:a%3Ab%40c@db.example/neondb?sslmode=require&channel_binding=require"));
        Assert.Equal(5432, result.Port); Assert.Equal("a:b@c", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode); Assert.Equal(ChannelBinding.Require, result.ChannelBinding);
    }
}

public sealed class BackendTests(TestServer server) : IClassFixture<TestServer>
{
    private HttpClient Client(string token = "")
    {
        var client = server.CreateClient();
        if (token != "") client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
    private static async Task<JsonElement> Ok(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    [Fact]
    public async Task AuthenticationPaymentsWalletRealtimeAndArena()
    {
        using var api = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("/api/me")).StatusCode);
        var health = await Ok(await api.GetAsync("/health")); Assert.Equal("ok", health.GetProperty("status").GetString());
        await Ok(await api.GetAsync("/openapi/v1.json"));
        using var cors = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        cors.Headers.Add("Origin", "https://whosabi.ng"); cors.Headers.Add("Access-Control-Request-Method", "POST");
        var corsResult = await api.SendAsync(cors); Assert.Equal("https://whosabi.ng", corsResult.Headers.GetValues("Access-Control-Allow-Origin").Single());
        var invalid = await api.PostAsJsonAsync("/api/auth/login", new { email = TestServer.QaEmail, password = "wrong" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var qaLogin = await Ok(await api.PostAsJsonAsync("/api/auth/login", new { email = TestServer.QaEmail, password = TestServer.QaPassword }));
        using var qa = Client(qaLogin.GetProperty("accessToken").GetString()!);
        var profile = await Ok(await qa.GetAsync("/api/me/profile")); Assert.True(profile.GetProperty("isTestAccount").GetBoolean());
        var me = await Ok(await qa.GetAsync("/api/me")); Assert.Equal(0, me.GetProperty("walletBalanceKobo").GetInt64());
        await Ok(await qa.GetAsync("/api/wallet")); await Ok(await qa.GetAsync("/api/wallet/transactions"));
        var unsafeConfirmation = await api.PostAsJsonAsync("/api/payments/signup/confirm", new { paymentReference = "unknown", providerReference = "invented", amountKobo = 250000 });
        Assert.Equal(HttpStatusCode.NotFound, unsafeConfirmation.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.PostAsJsonAsync("/api/payments/monnify/webhook", new { eventType = "SUCCESSFUL_TRANSACTION" })).StatusCode);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        async Task<(HttpClient Api, string Name, Guid Id, string Ref)> Register(string postfix, string phone)
        {
            var name = "player_" + suffix + postfix; var email = name + "@example.com";
            var input = new { username = name, email, password = TestServer.QaPassword, paymentMethod = "card", phoneNumber = phone, acceptedTerms = true, acceptedPrivacy = true, policyVersion = RegistrationProfile.CurrentPolicyVersion };
            var registration = await Ok(await api.PostAsJsonAsync("/api/auth/register/initiate", input));
            var reference = registration.GetProperty("paymentReference").GetString()!;
            Assert.StartsWith("https://sandbox.sdk.monnify.com/", registration.GetProperty("paymentUrl").GetString());
            Assert.Equal(HttpStatusCode.Conflict, (await api.PostAsJsonAsync("/api/payments/signup/confirm", new { paymentReference = reference, providerReference = "fake", amountKobo = 1 })).StatusCode);
            server.Provider.Paid[reference] = true;
            var confirmations = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => api.PostAsJsonAsync("/api/payments/signup/confirm", new { paymentReference = reference })));
            foreach (var response in confirmations) await Ok(response);
            var login = await Ok(await api.PostAsJsonAsync("/api/auth/login", new { identifier = name, password = TestServer.QaPassword }));
            return (Client(login.GetProperty("accessToken").GetString()!), name, login.GetProperty("userId").GetGuid(), reference);
        }
        var one = await Register("a", "+2348012345601"); var two = await Register("b", "+2348012345602");
        using var first = one.Api; using var second = two.Api;
        var phoneLogin = await Ok(await api.PostAsJsonAsync("/api/auth/login", new { identifier = "+2348012345601", password = TestServer.QaPassword }));
        Assert.Equal(one.Id, phoneLogin.GetProperty("userId").GetGuid());
        Assert.Equal(1, await server.WithDb(db => db.WalletLedgerEntries.CountAsync(x => x.UserId == one.Id && x.Reference == one.Ref)));
        server.Provider.OverrideEmail = "attacker@example.com";
        Assert.Equal(HttpStatusCode.Conflict, (await api.PostAsJsonAsync("/api/payments/signup/confirm", new { paymentReference = one.Ref })).StatusCode);
        server.Provider.OverrideEmail = null;
        var webhookJson = JsonSerializer.Serialize(new { eventType = "SUCCESSFUL_TRANSACTION", eventData = new { paymentReference = one.Ref } });
        var signature = Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes("test-only"), Encoding.UTF8.GetBytes(webhookJson)));
        for (var repeat = 0; repeat < 2; repeat++)
        {
            using var webhook = new HttpRequestMessage(HttpMethod.Post, "/api/payments/monnify/webhook") { Content = new StringContent(webhookJson, Encoding.UTF8, "application/json") };
            webhook.Headers.Add("monnify-signature", signature);
            Assert.Equal(HttpStatusCode.OK, (await api.SendAsync(webhook)).StatusCode);
        }
        Assert.Equal(1, await server.WithDb(db => db.WalletLedgerEntries.CountAsync(x => x.UserId == one.Id && x.Reference == one.Ref)));
        var transfer = new { username = two.Name, amountKobo = 10000, idempotencyKey = "transfer-once" };
        foreach (var result in await Task.WhenAll(first.PostAsJsonAsync("/api/wallet/transfers", transfer), first.PostAsJsonAsync("/api/wallet/transfers", transfer))) await Ok(result);
        Assert.Equal(40000, await server.WithDb(db => db.WalletAccounts.Where(x => x.UserId == one.Id).Select(x => x.BalanceKobo).SingleAsync()));
        Assert.Equal(HttpStatusCode.Conflict, (await first.PostAsJsonAsync("/api/wallet/transfers", new { username = two.Name, amountKobo = 50000, idempotencyKey = "overdraw-rejected" })).StatusCode);
        Assert.Equal(60000, await server.WithDb(db => db.WalletAccounts.Where(x => x.UserId == two.Id).Select(x => x.BalanceKobo).SingleAsync()));
        Assert.Equal(HttpStatusCode.Conflict, (await first.PostAsJsonAsync("/api/wallet/transfers", new { username = two.Name, amountKobo = 20000, idempotencyKey = "transfer-once" })).StatusCode);
        await Ok(await first.PostAsJsonAsync("/api/gifts/send", new { giftId = "clap", username = two.Name, idempotencyKey = "gift-once", priceKobo = 1 }));
        Assert.Equal(30000, await server.WithDb(db => db.WalletAccounts.Where(x => x.UserId == one.Id).Select(x => x.BalanceKobo).SingleAsync()));
        var deposit = await Ok(await first.PostAsJsonAsync("/api/wallet/deposits", new { amountKobo = 10000, idempotencyKey = "deposit-once" }));
        var depositRef = deposit.GetProperty("paymentReference").GetString()!; server.Provider.Paid[depositRef] = true;
        Assert.Equal(HttpStatusCode.NotFound, (await second.PostAsync($"/api/wallet/deposits/{depositRef}/verify", null)).StatusCode);
        foreach (var result in await Task.WhenAll(first.PostAsync($"/api/wallet/deposits/{depositRef}/verify", null), first.PostAsync($"/api/wallet/deposits/{depositRef}/verify", null))) await Ok(result);
        Assert.Equal(40000, await server.WithDb(db => db.WalletAccounts.Where(x => x.UserId == one.Id).Select(x => x.BalanceKobo).SingleAsync()));

        await Ok(await qa.PostAsJsonAsync("/api/rooms/qa-free-room/join", new { mode = "player" }));
        await using var hub = new HubConnectionBuilder().WithUrl(new Uri(qa.BaseAddress!, "/hubs/rooms"), options => {
            options.Transports = HttpTransportType.LongPolling; options.HttpMessageHandlerFactory = _ => server.Server.CreateHandler();
            options.AccessTokenProvider = () => Task.FromResult<string?>(qaLogin.GetProperty("accessToken").GetString());
        }).Build();
        await hub.StartAsync(); await hub.InvokeAsync<JsonElement>("SubscribeRoom", "qa-free-room");
        var started = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<JsonElement>("RoundStarted", state => started.TrySetResult(state));
        var round = await Ok(await qa.PostAsJsonAsync("/api/rooms/qa-free-room/rounds", new { questionId = "qa-question-1", durationSeconds = 30 }));
        var liveEvent = await started.Task.WaitAsync(TimeSpan.FromSeconds(10)); Assert.Equal(JsonValueKind.Null, liveEvent.GetProperty("correctIndex").ValueKind);
        Assert.Equal(HttpStatusCode.Forbidden, (await second.GetAsync("/api/rooms/qa-free-room/state")).StatusCode);
        var answer = await Ok(await qa.PostAsJsonAsync("/api/rooms/qa-free-room/answers", new { questionId = "qa-question-1", selectedIndex = 1 }));
        Assert.Equal(JsonValueKind.Null, answer.GetProperty("isCorrect").ValueKind); Assert.Equal(0, answer.GetProperty("rewardKobo").GetInt64());
        var roundId = round.GetProperty("roundId").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await qa.GetAsync($"/api/rooms/qa-free-room/rounds/{roundId}/results")).StatusCode);
        await Ok(await qa.PostAsync($"/api/rooms/qa-free-room/rounds/{roundId}/close", null));
        var results = await Ok(await qa.GetAsync($"/api/rooms/qa-free-room/rounds/{roundId}/results")); Assert.Equal(1, results.GetProperty("correctIndex").GetInt32());
        await Ok(await qa.PostAsJsonAsync("/api/rooms/qa-free-room/chat", new { text = "Hello", clientMessageId = "hello-once" }));
        await Ok(await qa.PostAsJsonAsync("/api/rooms/qa-free-room/chat", new { text = "Hello", clientMessageId = "hello-once" }));
        var history = await Ok(await qa.GetAsync("/api/rooms/qa-free-room/chat")); Assert.Single(history.EnumerateArray());
        await hub.StopAsync();
        var timedQuestion = await Ok(await qa.PostAsJsonAsync("/api/rooms/qa-free-room/questions", new { category = "QA", prompt = "Timed question", options = new[] { "A", "B" }, correctIndex = 0 }));
        var timedId = timedQuestion.GetProperty("id").GetString();
        var timedRound = await Ok(await qa.PostAsJsonAsync("/api/rooms/qa-free-room/rounds", new { questionId = timedId, durationSeconds = 5 }));
        await Task.Delay(TimeSpan.FromSeconds(6));
        Assert.Equal(HttpStatusCode.BadRequest, (await qa.PostAsJsonAsync("/api/rooms/qa-free-room/answers", new { questionId = timedId, selectedIndex = 0 })).StatusCode);
        await Ok(await qa.GetAsync($"/api/rooms/qa-free-room/rounds/{timedRound.GetProperty("roundId").GetGuid()}/results"));

        var arena = await Ok(await first.PostAsJsonAsync("/api/tournaments", new { title = "Private test arena", inviteOnly = true, entryFeeKobo = 0, startsAt = DateTimeOffset.UtcNow.AddMinutes(5) }));
        var arenaId = arena.GetProperty("id").GetGuid(); var roomId = arena.GetProperty("roomId").GetString();
        Assert.Equal(HttpStatusCode.Forbidden, (await second.GetAsync($"/api/tournaments/{arenaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await second.GetAsync($"/api/rooms/{roomId}")).StatusCode);
        var invitation = await Ok(await first.PostAsJsonAsync($"/api/tournaments/{arenaId}/invites", new { username = two.Name }));
        var inviteId = invitation.GetProperty("invite").GetProperty("id").GetGuid();
        await Ok(await second.PostAsync($"/api/invites/{inviteId}/accept", null));
        await Ok(await second.PostAsync($"/api/tournaments/{arenaId}/enter", null));
        await Ok(await second.PostAsync($"/api/tournaments/{arenaId}/enter", null));
        Assert.Equal(1, await server.WithDb(db => db.Set<TournamentEntry>().CountAsync(x => x.TournamentId == arenaId)));
        Assert.Equal(HttpStatusCode.Conflict, (await first.PostAsync($"/api/invites/{inviteId}/revoke", null)).StatusCode);
        await Ok(await first.PostAsync($"/api/tournaments/{arenaId}/cancel", null));
        await Ok(await first.PostAsync($"/api/tournaments/{arenaId}/cancel", null));
        var unread = await Ok(await second.GetAsync("/api/notifications/unread-count")); Assert.True(unread.GetProperty("count").GetInt32() >= 1);
        await second.PostAsync("/api/notifications/read-all", null);
        Assert.Equal(0, (await Ok(await second.GetAsync("/api/notifications/unread-count"))).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await qa.PostAsync("/api/wallet/withdrawals", null)).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await qa.PostAsync("/api/rooms/qa-free-room/media/token", null)).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await api.PostAsJsonAsync("/api/auth/google", new { idToken = "fake" })).StatusCode);

        var beforePool = await server.WithDb(db => db.WalletAccounts.Where(x => x.UserId == one.Id || x.UserId == two.Id).SumAsync(x => x.BalanceKobo));
        var paidArena = await Ok(await first.PostAsJsonAsync("/api/tournaments", new { title = "Funded test arena", inviteOnly = false, entryFeeKobo = 10000, startsAt = DateTimeOffset.UtcNow.AddMinutes(5) }));
        var paidId = paidArena.GetProperty("id").GetGuid(); var paidRoom = paidArena.GetProperty("roomId").GetString();
        Assert.Equal(HttpStatusCode.Forbidden, (await first.PostAsync($"/api/tournaments/{paidId}/enter", null)).StatusCode);
        await Ok(await second.PostAsync($"/api/tournaments/{paidId}/enter", null));
        await Ok(await second.PostAsJsonAsync($"/api/tournaments/{paidId}/contributions", new { amountKobo = 5000, idempotencyKey = "pool-once" }));
        await Ok(await second.PostAsJsonAsync($"/api/tournaments/{paidId}/contributions", new { amountKobo = 5000, idempotencyKey = "pool-once" }));
        var q = await Ok(await first.PostAsJsonAsync($"/api/rooms/{paidRoom}/questions", new { category = "test", prompt = "Capital of Nigeria?", options = new[] { "Abuja", "Lagos" }, correctIndex = 0 }));
        var qId = q.GetProperty("id").GetString();
        await server.WithDb(async db => {
            var item = await db.Set<Tournament>().SingleAsync(x => x.Id == paidId); item.StartsAt = DateTimeOffset.UtcNow.AddMinutes(-1); return await db.SaveChangesAsync();
        });
        var paidRound = await Ok(await first.PostAsJsonAsync($"/api/rooms/{paidRoom}/rounds", new { questionId = qId, durationSeconds = 30 }));
        await Ok(await second.PostAsJsonAsync($"/api/rooms/{paidRoom}/answers", new { questionId = qId, selectedIndex = 0 }));
        await Ok(await first.PostAsync($"/api/rooms/{paidRoom}/rounds/{paidRound.GetProperty("roundId").GetGuid()}/close", null));
        await Ok(await first.PostAsync($"/api/tournaments/{paidId}/settle", null));
        await Ok(await first.PostAsync($"/api/tournaments/{paidId}/settle", null));
        Assert.Equal(beforePool, await server.WithDb(db => db.WalletAccounts.Where(x => x.UserId == one.Id || x.UserId == two.Id).SumAsync(x => x.BalanceKobo)));
        Assert.Equal(1, await server.WithDb(db => db.WalletLedgerEntries.CountAsync(x => x.UserId == two.Id && x.Reference == $"SETTLE-{paidId:N}-{two.Id:N}")));
    }
}
