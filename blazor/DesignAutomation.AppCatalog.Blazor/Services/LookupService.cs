using DesignAutomation.AppCatalog.Blazor.Models.Api;
using DesignAutomation.AppCatalog.Blazor.Services.Api;
using Microsoft.Extensions.Logging;

namespace DesignAutomation.AppCatalog.Blazor.Services;

public class LookupService
{
    private readonly AppCatalogApiClient _apiClient;
    private readonly CacheService _cache;
    private readonly ILogger<LookupService> _logger;

    public LookupService(
        AppCatalogApiClient apiClient,
        CacheService cache,
        ILogger<LookupService> logger)
    {
        _apiClient = apiClient;
        _cache = cache;
        _logger = logger;
    }

    public IReadOnlyList<EnumItem> AvailabilityOptions { get; private set; } = [];

    public async Task<LookupCollection> GetAsync()
    {
        var lookups = await _cache.GetOrCreateAsync(CacheService.LookupsKey, FetchAsync);
        AvailabilityOptions = lookups.Availability;
        return lookups;
    }

    public void Refresh() => _cache.Invalidate(CacheService.LookupsKey);

    private async Task<LookupCollection> FetchAsync()
    {
        try
        {
            var result = await _apiClient.GetLookupsAsync();
            return result.IsSuccess && result.ResultObject is not null
                ? result.ResultObject
                : new LookupCollection();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load lookups.");
            return new LookupCollection();
        }
    }
}
