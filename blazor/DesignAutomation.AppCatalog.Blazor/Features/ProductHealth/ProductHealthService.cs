using DesignAutomation.AppCatalog.Blazor.Services.Api;
using Microsoft.Extensions.Logging;

namespace DesignAutomation.AppCatalog.Blazor.Features.ProductHealth;

public class ProductHealthService
{
    private readonly AppCatalogApiClient _apiClient;
    private readonly ILogger<ProductHealthService> _logger;

    public ProductHealthService(AppCatalogApiClient apiClient, ILogger<ProductHealthService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<ProductHealthMetrics> GetMetricsAsync()
    {
        _logger.LogInformation("Loading product health metrics");

        try
        {
            var result = await _apiClient.GetAppsAsync();
            var apps = result.IsSuccess && result.ResultObject is not null
                ? result.ResultObject
                : [];

            // Stub health classification — extend when health data is available
            return new ProductHealthMetrics(
                GeneratedAt: DateTimeOffset.UtcNow,
                TotalApps: apps.Count,
                HealthyApps: apps.Count(a => a.Production),
                DegradedApps: 0,
                UnknownApps: apps.Count(a => !a.Production)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to compute product health metrics");
            return new ProductHealthMetrics(DateTimeOffset.UtcNow, 0, 0, 0, 0);
        }
    }
}
