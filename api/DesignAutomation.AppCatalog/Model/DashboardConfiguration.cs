using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

public sealed record DashboardModuleDefinition(
    [property: JsonProperty("key")] string Key,
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("defaultEnabled")] bool DefaultEnabled);

public sealed record DashboardWidgetConfiguration
{
    [JsonProperty("visible")] public bool Visible { get; init; } = true;
    [JsonProperty("size")] public string Size { get; init; } = "medium";
}

public sealed record DashboardLayout
{
    [JsonProperty("columns")] public int Columns { get; init; } = 2;
}

public sealed record DashboardDefaultFilters
{
    [JsonProperty("owner")] public string Owner { get; init; } = string.Empty;
    [JsonProperty("status")] public string Status { get; init; } = string.Empty;
    [JsonProperty("team")] public string Team { get; init; } = string.Empty;
}

public sealed record DashboardDefaultDateRange
{
    [JsonProperty("preset")] public string Preset { get; init; } = "30d";
    [JsonProperty("startDate")] public DateOnly? StartDate { get; init; }
    [JsonProperty("endDate")] public DateOnly? EndDate { get; init; }
}

public sealed record DashboardConfiguration
{
    [JsonProperty("productId")] public Guid ProductId { get; init; }
    [JsonProperty("modules")] public List<DashboardModuleDefinition> Modules { get; init; } = [];
    [JsonProperty("enabledModules")] public List<string> EnabledModules { get; init; } = [];
    [JsonProperty("moduleOrder")] public List<string> ModuleOrder { get; init; } = [];
    [JsonProperty("widgetConfiguration")] public Dictionary<string, DashboardWidgetConfiguration> WidgetConfiguration { get; init; } = new(StringComparer.Ordinal);
    [JsonProperty("dashboardLayout")] public DashboardLayout DashboardLayout { get; init; } = new();
    [JsonProperty("defaultFilters")] public DashboardDefaultFilters DefaultFilters { get; init; } = new();
    [JsonProperty("defaultDateRange")] public DashboardDefaultDateRange DefaultDateRange { get; init; } = new();
}

public interface IDashboardConfigurationRepository
{
    Task<DashboardConfiguration?> Get(Guid productId);
    Task<bool> Save(Guid productId, DashboardConfiguration configuration);
}
