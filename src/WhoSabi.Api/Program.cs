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

var builder = WebApplication.CreateBuilder(args);

// Render supplies the listening port at runtime. Binding explicitly also keeps
// the container usable on other hosting platforms.
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddScoped<IInitiateRegistrationUseCase, InitiateRegistrationUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IGetCurrentUserUseCase, GetCurrentUserUseCase>();
builder.Services.AddScoped<IGetWalletUseCase, GetWalletUseCase>();
builder.Services.AddScoped<IGetWalletTransactionsUseCase, GetWalletTransactionsUseCase>();
builder.Services.AddScoped<IGetLiveRoomsUseCase, GetLiveRoomsUseCase>();
builder.Services.AddScoped<IGetRoomDetailUseCase, GetRoomDetailUseCase>();
builder.Services.AddScoped<IJoinRoomUseCase, JoinRoomUseCase>();
builder.Services.AddScoped<ISubmitRoomAnswerUseCase, SubmitRoomAnswerUseCase>();
builder.Services.AddScoped<IConfirmSignupPaymentUseCase, ConfirmSignupPaymentUseCase>();
builder.Services.AddInfrastructure(builder.Configuration);
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
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Render terminates TLS before proxying requests to this container.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Apply the checked-in EF Core migrations before serving traffic. This makes a
// fresh Render Postgres instance ready without a separate manual migration step.
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WhoSabiDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapAuthEndpoints();
app.MapMeEndpoints();
app.MapPaymentEndpoints();
app.MapRoomEndpoints();
app.MapWalletEndpoints();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
