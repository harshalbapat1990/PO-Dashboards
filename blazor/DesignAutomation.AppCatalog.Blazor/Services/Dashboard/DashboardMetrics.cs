namespace DesignAutomation.AppCatalog.Blazor.Services.Dashboard;

public record DashboardMetrics
{
    public int TotalApps { get; init; }
    public int ProductionApps { get; init; }
    public int NonProductionApps { get; init; }
    public int Repositories { get; init; }
    public int PoCs { get; init; }
    public int Users { get; init; }
    public IReadOnlyDictionary<string, int> AvailabilityDistribution { get; init; } =
        new Dictionary<string, int>();
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}
