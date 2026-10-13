using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DesignAutomation.AppCatalog.Blazor.Services.Authentication;

public class AppAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;

    public AppAuthenticationStateProvider(AuthService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _authService.GetCurrentUserAsync();

        if (token is null || !token.IsValid)
            return Unauthenticated();

        var claims = new List<Claim>();

        if (!string.IsNullOrEmpty(token.UserId))
            claims.Add(new Claim(ClaimTypes.NameIdentifier, token.UserId));

        if (!string.IsNullOrEmpty(token.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, token.Role));
            claims.Add(new Claim(ClaimTypes.Name, token.UserId ?? string.Empty));
        }

        if (!string.IsNullOrEmpty(token.Picture))
            claims.Add(new Claim("picture", token.Picture));

        var identity = new ClaimsIdentity(claims, "jwt");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Unauthenticated()));
    }

    private static AuthenticationState Unauthenticated() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
