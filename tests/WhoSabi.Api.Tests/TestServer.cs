using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using WhoSabi.Api.Platform;
using WhoSabi.Infrastructure;
using WhoSabi.Infrastructure.Persistence;
using Xunit;

namespace WhoSabi.Api.Tests;

public sealed class TestServer : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string connection = "";
    private string adminConnection = "";
    private bool created;
    private readonly string database = "whosabi_tests_" + Guid.NewGuid().ToString("N");
    public TestMonnify Provider { get; } = new();
    public HttpClient Api { get; private set; } = null!;
    public const string QaEmail = "qa-backend-tests@whosabi.ng";
    public const string QaPassword = "This-Is-Only-A-Test-Password-1234!";

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
            ?? throw new InvalidOperationException("TEST_DATABASE_URL must permit creation of a temporary PostgreSQL database.");
        adminConnection = DependencyInjection.ToNpgsqlConnectionString(configured);
        await using var db = new NpgsqlConnection(adminConnection); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand($"CREATE DATABASE {database}", db); await cmd.ExecuteNonQueryAsync();
        created = true;
        connection = new NpgsqlConnectionStringBuilder(adminConnection) { Database = database, SearchPath = null }.ConnectionString;
        await using (var isolated = new NpgsqlConnection(connection))
        {
            await isolated.OpenAsync();
            await TestDatabaseGuard.AssertDatabase(isolated, database, default);
        }
        Api = CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = connection,
            ["Jwt:SigningKey"] = "This-Is-Only-A-Test-Signing-Key-Do-Not-Use-In-Production-0123456789",
            ["TestAccount:Enabled"] = "true", ["TestAccount:Email"] = QaEmail, ["TestAccount:Password"] = QaPassword,
            ["Monnify:ApiKey"] = "test-only", ["Monnify:SecretKey"] = "test-only", ["Monnify:ContractCode"] = "test-only",
            ["Wallet:TransfersEnabled"] = "true"
        }));
        builder.ConfigureServices(services => {
            services.ConfigureDbContext<WhoSabiDbContext>((_, options) => options.AddInterceptors(new TestDatabaseGuard(database)));
            services.RemoveAll<MonnifyClient>();
            services.AddScoped(sp => new MonnifyClient(new HttpClient(Provider, disposeHandler: false), sp.GetRequiredService<IConfiguration>()));
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Api?.Dispose(); await base.DisposeAsync();
        if (!created) return;
        if (!System.Text.RegularExpressions.Regex.IsMatch(database, "^whosabi_tests_[a-f0-9]{32}$")) throw new InvalidOperationException();
        NpgsqlConnection.ClearAllPools();
        await using var db = new NpgsqlConnection(adminConnection); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", db); await cmd.ExecuteNonQueryAsync();
    }

    public async Task<T> WithDb<T>(Func<WhoSabiDbContext, Task<T>> callback)
    {
        await using var scope = Services.CreateAsyncScope(); return await callback(scope.ServiceProvider.GetRequiredService<WhoSabiDbContext>());
    }
}

public sealed class TestDatabaseGuard(string expected) : Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor
{
    public static async Task AssertDatabase(System.Data.Common.DbConnection connection, string expected, CancellationToken ct)
    {
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT current_database()";
        if ((string?)await command.ExecuteScalarAsync(ct) != expected)
            throw new InvalidOperationException("Refusing test writes: actual database does not match the dedicated test database.");
    }
    public override Task ConnectionOpenedAsync(System.Data.Common.DbConnection connection, Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        => AssertDatabase(connection, expected, cancellationToken);
    public override void ConnectionOpened(System.Data.Common.DbConnection connection, Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData)
        => AssertDatabase(connection, expected, default).GetAwaiter().GetResult();
}

// Provider simulation is confined to this test assembly. Production never accepts fake payment success.
public sealed class TestMonnify : HttpMessageHandler
{
    public sealed record Session(decimal Amount, string Email);
    public ConcurrentDictionary<string, Session> Sessions { get; } = new();
    public ConcurrentDictionary<string, bool> Paid { get; } = new();
    public string? OverrideEmail { get; set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        object body;
        if (request.RequestUri!.AbsolutePath.EndsWith("/auth/login")) body = new { accessToken = "test-provider-token" };
        else if (request.RequestUri.AbsolutePath.EndsWith("init-transaction"))
        {
            var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct)).RootElement;
            var reference = json.GetProperty("paymentReference").GetString()!;
            Sessions[reference] = new Session(json.GetProperty("amount").GetDecimal(), json.GetProperty("customerEmail").GetString()!);
            body = new { paymentReference = reference, transactionReference = "TEST-" + reference, checkoutUrl = "https://sandbox.sdk.monnify.com/checkout/" + reference };
        }
        else
        {
            var reference = Uri.UnescapeDataString(request.RequestUri.Query.Split('=', 2)[1]); var session = Sessions[reference];
            body = new { paymentReference = reference, transactionReference = "TEST-" + reference, paymentStatus = Paid.GetValueOrDefault(reference) ? "PAID" : "PENDING", amountPaid = session.Amount, currency = "NGN", customer = new { email = OverrideEmail ?? session.Email } };
        }
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { requestSuccessful = true, responseBody = body }) };
    }
}
