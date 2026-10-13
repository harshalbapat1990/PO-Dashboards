using DesignAutomation.AppCatalog.Blazor.Models.Auth;
using DesignAutomation.AppCatalog.Blazor.Services.Storage;
using Microsoft.Extensions.Logging;

namespace DesignAutomation.AppCatalog.Blazor.Services.Authentication;

public class AuthService
{
    private readonly ITokenStorage _tokenStorage;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ITokenStorage tokenStorage, ILogger<AuthService> logger)
    {
        _tokenStorage = tokenStorage;
        _logger = logger;
    }

    public async Task<AuthToken?> GetCurrentUserAsync()
    {
        var token = await _tokenStorage.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var auth = AuthToken.Create(token);

            return auth.IsValid
                ? auth
                : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var user = await GetCurrentUserAsync();

        return user?.IsValid ?? false;
    }

    public Task LogoutAsync()
    {
        _logger.LogInformation("User logging out");
        return _tokenStorage.DeleteTokenAsync();
    }
}