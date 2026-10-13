using Microsoft.Extensions.Caching.Memory;

namespace DesignAutomation.AppCatalog.Blazor.Services;

public class CacheService
{
    public const string GithubReposKey = "github-repos";
    public const string LookupsKey = "lookups";
    public const string DashboardMetricsKey = "dashboard-metrics";

    private static readonly TimeSpan DefaultSliding = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? sliding = null)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory();

        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            SlidingExpiration = sliding ?? DefaultSliding
        });

        return value;
    }

    public void Invalidate(string key) => _cache.Remove(key);
}
