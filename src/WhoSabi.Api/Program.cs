using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WhoSabi.Api.Auth;
using WhoSabi.Api.Me;
using WhoSabi.Api.Payments;
using WhoSabi.Api.Rooms;
using WhoSabi.Api.Wallets;
using WhoSabi.Application.Auth;
using WhoSabi.Application.Payments;
using WhoSabi.Application.Rooms;
using WhoSabi.Application.Users;
using WhoSabi.Application.Wallets;
using WhoSabi.Infrastructure;
using WhoSabi.Infrastructure.Persistence;
using WhoSabi.Api.Platform;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Render supplies the listening port at runtime. Binding explicitly also keeps
// the container usable on other hosting platforms.
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 65_536);

builder.Services.AddScoped<IInitiateRegistrationUseCase, InitiateRegistrationUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IGetCurrentUserUseCase, GetCurrentUserUseCase>();
builder.Services.AddScoped<IGetWalletUseCase, GetWalletUseCase>();
builder.Services.AddScoped<IGetWalletTransactionsUseCase, GetWalletTransactionsUseCase>();
builder.Services.AddScoped<IGetLiveRoomsUseCase, GetLiveRoomsUseCase>();
builder.Services.AddScoped<IGetRoomDetailUseCase, GetRoomDetailUseCase>();
builder.Services.AddScoped<IJoinRoomUseCase, JoinRoomUseCase>();
builder.Services.AddScoped<ISubmitRoomAnswerUseCase, SubmitRoomAnswerUseCase>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyMethod().AllowAnyHeader().AllowCredentials()));
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1) }));
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<RoomConnections>();
builder.Services.AddSingleton<RoundSchedule>();
builder.Services.AddHttpClient<MonnifyClient>(http => http.Timeout = TimeSpan.FromSeconds(25));
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<RoundService>();
builder.Services.AddSingleton<GoogleTokenVerifier>();
builder.Services.AddScoped<TournamentService>();
builder.Services.AddHostedService<RoundClock>();
builder.Services.AddHttpClient<FirebasePush>(http => http.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHostedService<PushDispatcher>();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKey = builder.Configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents { OnMessageReceived = context => {
            if (context.Request.Path.StartsWithSegments("/hubs/rooms") && context.Request.Query.TryGetValue("access_token", out var token)) context.Token = token;
            return Task.CompletedTask;
        }};
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseApiErrors();

// Render terminates TLS before proxying requests to this container.
var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, ForwardLimit = 1 };
// Render's container ingress is reached through its managed edge proxy.
if (Environment.GetEnvironmentVariable("RENDER") == "true")
{
    forwarded.KnownProxies.Clear(); forwarded.KnownIPNetworks.Clear();
}
app.UseForwardedHeaders(forwarded);

// Apply the checked-in EF Core migrations before serving traffic. This makes a
// fresh Render Postgres instance ready without a separate manual migration step.
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WhoSabiDbContext>();
    await dbContext.Database.MigrateAsync();
    await TestAccountSeed.Apply(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
app.MapOpenApi();

// Render's edge enforces HTTPS. Kestrel listens on HTTP behind that edge.
// Local launch profiles use HTTP; production clients always use the public HTTPS URL.
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapAuthEndpoints();
app.MapMeEndpoints();
app.MapPaymentEndpoints();
app.MapRoomEndpoints();
app.MapWalletEndpoints();
app.MapPlatformWallet();
app.MapProfileAndNotifications();
app.MapRoundsAndChat();
app.MapTournaments();
app.MapMedia();
app.MapGoogleAuth();
app.MapHub<RoomHub>("/hubs/rooms", options => options.CloseOnAuthenticationExpiration = true);
app.MapGet("/api/config", (IConfiguration config, MonnifyClient monnify, FirebasePush push) => Results.Ok(new {
    apiBaseUrl = config["PublicApiUrl"], policyVersion = RegistrationProfile.CurrentPolicyVersion,
    signupActivationAmountKobo = 250_000, activationBonusKobo = 50_000, referralBonusKobo = 0,
    paymentsConfigured = monnify.Configured, transfersEnabled = config.GetValue<bool>("Wallet:TransfersEnabled"),
    googleConfigured = config.GetSection("Google:ClientIds").Get<string[]>()?.Length > 0,
    mediaConfigured = !string.IsNullOrEmpty(config["LiveKit:ApiSecret"]), pushConfigured = push.Configured, withdrawalsConfigured = false,
    roomHubUrl = "https://api.whosabi.ng/hubs/rooms"
}));

app.Run();
public partial class Program { }
