using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DesignAutomation.AppCatalog.Blazor.Services.Storage;

public class SessionStorageService : ITokenStorage
{
    private const string TokenKey = "token";

    private readonly ProtectedSessionStorage _storage;

    public SessionStorageService(
        ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public async Task<string?> GetTokenAsync()
    {
        var result =
            await _storage.GetAsync<string>(TokenKey);

        return result.Success
            ? result.Value
            : null;
    }

    public async Task SetTokenAsync(
        string token,
        DateTimeOffset? expires = null)
    {
        await _storage.SetAsync(TokenKey, token);
    }

    public async Task DeleteTokenAsync()
    {
        await _storage.DeleteAsync(TokenKey);
    }
}