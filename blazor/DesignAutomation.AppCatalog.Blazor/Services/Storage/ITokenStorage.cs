namespace DesignAutomation.AppCatalog.Blazor.Services.Storage;

public interface ITokenStorage
{
    Task<string?> GetTokenAsync();

    Task SetTokenAsync(
        string token,
        DateTimeOffset? expires = null);

    Task DeleteTokenAsync();
}
