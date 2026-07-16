using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WhoSabi.Application.Auth;

namespace WhoSabi.Infrastructure.Auth;

public sealed class JwtAccessTokenService(IConfiguration configuration) : IAccessTokenService
{
    public AccessToken CreateAccessToken(LoginUserRecord user)
    {
        var issuer = GetRequiredSetting("Jwt:Issuer");
        var audience = GetRequiredSetting("Jwt:Audience");
        var signingKey = GetRequiredSetting("Jwt:SigningKey");
        var accessTokenMinutes = GetAccessTokenMinutes();

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(accessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return new AccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    private string GetRequiredSetting(string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is missing.");
        }

        return value;
    }

    private int GetAccessTokenMinutes()
    {
        var configuredValue = configuration["Jwt:AccessTokenMinutes"];
        return int.TryParse(configuredValue, out var minutes) && minutes > 0
            ? minutes
            : 60;
    }
}
