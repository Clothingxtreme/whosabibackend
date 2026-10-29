using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WhoSabi.Application.Auth;
using WhoSabi.Contracts.Auth;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public sealed record GoogleTokenRequest(string IdToken);

public sealed class GoogleTokenVerifier(IConfiguration config)
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> metadata = new(
        "https://accounts.google.com/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());

    public async Task<ClaimsPrincipal> Verify(string token, CancellationToken ct)
    {
        var clients = config.GetSection("Google:ClientIds").Get<string[]>() ?? [];
        if (clients.Length == 0) throw new ApiFault(503, "GOOGLE_NOT_CONFIGURED", "Configure Google OAuth client IDs first.");
        if (string.IsNullOrWhiteSpace(token) || token.Length > 16_384) throw new ApiFault(400, "INVALID_GOOGLE_TOKEN", "An ID token is required.");
        var info = await metadata.GetConfigurationAsync(ct);
        try { return Validate(info.SigningKeys); }
        catch (SecurityTokenSignatureKeyNotFoundException)
        {
            metadata.RequestRefresh(); info = await metadata.GetConfigurationAsync(ct);
            try { return Validate(info.SigningKeys); }
            catch (SecurityTokenException) { throw new ApiFault(401, "INVALID_GOOGLE_TOKEN", "Google ID token validation failed."); }
        }
        catch (Exception e) when (e is SecurityTokenException or ArgumentException)
        { throw new ApiFault(401, "INVALID_GOOGLE_TOKEN", "Google ID token validation failed."); }

        ClaimsPrincipal Validate(IEnumerable<SecurityKey> keys)
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            return handler.ValidateToken(token, new TokenValidationParameters {
                ValidIssuers = ["https://accounts.google.com", "accounts.google.com"], ValidAudiences = clients,
                IssuerSigningKeys = keys, ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
                RequireSignedTokens = true, ValidateIssuerSigningKey = true, ClockSkew = TimeSpan.FromSeconds(30),
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
            }, out _);
        }
    }
}

public static class GoogleAuthEndpoints
{
    public static void MapGoogleAuth(this WebApplication app)
    {
        app.MapPost("/api/auth/google", async (GoogleTokenRequest input, GoogleTokenVerifier verifier, WhoSabiDbContext db, ILoginDataStore login, IAccessTokenService tokens, CancellationToken ct) => {
            var identity = await verifier.Verify(input.IdToken, ct);
            var subject = identity.FindFirstValue("sub");
            if (string.IsNullOrEmpty(subject)) throw new ApiFault(401, "INVALID_GOOGLE_TOKEN", "Missing Google subject.");
            var user = await db.Users.SingleOrDefaultAsync(x => x.GoogleSubject == subject, ct)
                ?? throw new ApiFault(409, "GOOGLE_ACCOUNT_NOT_LINKED", "Complete registration and link Google from your signed-in profile first.");
            var record = await login.FindUserByEmailAsync(user.NormalizedEmail, ct);
            if (record?.WalletBalanceKobo is null) throw new ApiFault(409, "ACCOUNT_INCOMPLETE", "Account wallet missing.");
            var token = tokens.CreateAccessToken(record);
            return Results.Ok(new LoginResponse(user.Id, user.Username, user.Email, record.WalletBalanceKobo.Value, token.Token, "Bearer", token.ExpiresAt));
        }).RequireRateLimiting("auth");
        app.MapPost("/api/me/auth/google", async (GoogleTokenRequest input, ClaimsPrincipal principal, GoogleTokenVerifier verifier, WhoSabiDbContext db, CancellationToken ct) => {
            var identity = await verifier.Verify(input.IdToken, ct);
            var uid = principal.UserId(); var user = await db.Users.SingleAsync(x => x.Id == uid, ct);
            if (!string.Equals(identity.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(identity.FindFirstValue("email"), user.Email, StringComparison.OrdinalIgnoreCase))
                throw new ApiFault(400, "GOOGLE_EMAIL_MISMATCH", "Google email must be verified and match the signed-in account.");
            user.GoogleSubject = ApiSupport.Required(identity.FindFirstValue("sub"), 255, "Google subject");
            await db.SaveChangesAsync(ct); return Results.Ok(new { linked = true });
        }).RequireAuthorization();
    }
}
