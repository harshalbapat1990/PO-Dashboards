using DesignAutomation.AppCatalog.Blazor.Models.Api;
using DesignAutomation.AppCatalog.Blazor.Services.Api;
using Microsoft.Extensions.Logging;

namespace DesignAutomation.AppCatalog.Blazor.Services.Dashboard;

public class DashboardService
{
    private readonly AppCatalogApiClient _apiClient;
    private readonly CacheService _cache;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        AppCatalogApiClient apiClient,
        CacheService cache,
        ILogger<DashboardService> logger)
    {
        _apiClient = apiClient;
        _cache = cache;
        _logger = logger;
    }

    public Task<DashboardMetrics> GetMetricsAsync() =>
        _cache.GetOrCreateAsync(CacheService.DashboardMetricsKey, BuildMetricsAsync);

    public void Refresh() => _cache.Invalidate(CacheService.DashboardMetricsKey);

    private async Task<DashboardMetrics> BuildMetricsAsync()
    {
        var appsTask   = SafeAsync(_apiClient.GetAppsAsync);
        var reposTask  = SafeAsync(_apiClient.GetGithubReposAsync);
        var pocsTask   = SafeAsync(_apiClient.GetPoCsAsync);
        var usersTask  = SafeAsync(_apiClient.GetUsersAsync);

        await Task.WhenAll(appsTask, reposTask, pocsTask, usersTask);

        var apps = appsTask.Result ?? [];

        var distribution = apps
            .GroupBy(a => a.Availability.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new DashboardMetrics
        {
            TotalApps = apps.Count,
            ProductionApps = apps.Count(a => a.Production),
            NonProductionApps = apps.Count(a => !a.Production),
            Repositories = reposTask.Result?.Count ?? 0,
            PoCs = pocsTask.Result?.Count ?? 0,
            Users = usersTask.Result?.Count ?? 0,
            AvailabilityDistribution = distribution,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<List<T>?> SafeAsync<T>(Func<Task<APIResult<List<T>>>> call)
    {
        try
        {
            var result = await call();
            return result.IsSuccess ? result.ResultObject : [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dashboard metric source failed: {Source}", typeof(T).Name);
            return [];
        }
    }
}
