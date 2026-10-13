namespace DesignAutomation.AppCatalog.Blazor.Features.ProductHealth;

/// <summary>Extension point: aggregate health signals per app</summary>
public record ProductHealthMetrics(
    DateTimeOffset GeneratedAt,
    int TotalApps,
    int HealthyApps,
    int DegradedApps,
    int UnknownApps
);
