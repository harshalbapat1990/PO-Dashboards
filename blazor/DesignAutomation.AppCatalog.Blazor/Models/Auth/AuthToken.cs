using System.IdentityModel.Tokens.Jwt;

namespace DesignAutomation.AppCatalog.Blazor.Models.Auth;

public class AuthToken
{
    public string RawToken { get; init; } = string.Empty;

    public string? UserId { get; init; }

    public string? Role { get; init; }

    public string? Picture { get; init; }

    public DateTimeOffset Expires { get; init; }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(RawToken) &&
        DateTimeOffset.UtcNow < Expires;

    public bool IsAuthorized(UserRole role)
    {
        if (!Enum.TryParse<UserRole>(Role, true, out var current))
            return false;

        return current >= role;
    }

    public static AuthToken Create(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();

        var token = handler.ReadJwtToken(jwt);

        var expClaim = token.Claims.FirstOrDefault(c => c.Type == "exp");

        long exp = expClaim != null
            ? long.Parse(expClaim.Value)
            : 0;

        return new AuthToken
        {
            RawToken = jwt,
            UserId = token.Claims.FirstOrDefault(c => c.Type == "id")?.Value,
            Role = token.Claims.FirstOrDefault(c => c.Type == "role")?.Value,
            Picture = token.Claims.FirstOrDefault(c => c.Type == "picture")?.Value,
            Expires = DateTimeOffset.FromUnixTimeSeconds(exp)
        };
    }
}