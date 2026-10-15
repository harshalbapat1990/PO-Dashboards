using DesignAutomation.AppCatalog.Model;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace DesignAutomation.AppCatalog.Data;

public sealed class PostgresDashboardConfigurationRepository(NpgsqlDataSource dataSource) : IDashboardConfigurationRepository
{
    private static readonly DashboardModuleDefinition[] ModuleRegistry =
    [
        new("executive-overview", "Executive Overview", true),
        new("sprint-health", "Sprint Health", true),
        new("pull-requests", "Pull Requests", true),
        new("security", "Security", true),
        new("testing", "Testing", true),
        new("deployment", "Deployment", true),
        new("technical-debt", "Technical Debt", true),
        new("ai-insights", "AI Insights", true)
    ];

    private static readonly string[] ModuleKeys = ModuleRegistry.Select(x => x.Key).ToArray();

    public async Task<DashboardConfiguration?> Get(Guid productId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT enabled_modules::text,module_order::text,widget_configuration::text,dashboard_layout::text,default_filters::text,default_date_range::text FROM product_dashboard_configuration WHERE product_id=@product", connection);
        command.Parameters.AddWithValue("product", productId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            await reader.DisposeAsync();
            await using var exists = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM products WHERE id=@product)", connection);
            exists.Parameters.AddWithValue("product", productId);
            return (bool)(await exists.ExecuteScalarAsync() ?? false) ? Default(productId) : null;
        }
        var widgets = DefaultWidgets();
        var storedWidgets = ReadJson<Dictionary<string, DashboardWidgetConfiguration>>(reader.GetString(2), new(StringComparer.Ordinal));
        foreach (var (key, widget) in storedWidgets.Where(item => ModuleKeys.Contains(item.Key, StringComparer.Ordinal)))
            widgets[key] = widget;

        var configuration = new DashboardConfiguration
        {
            ProductId = productId,
            Modules = ModuleRegistry.ToList(),
            EnabledModules = ReadJson<List<string>>(reader.GetString(0), ModuleKeys.ToList()),
            ModuleOrder = ReadJson<List<string>>(reader.GetString(1), ModuleKeys.ToList()),
            WidgetConfiguration = widgets,
            DashboardLayout = ReadJson<DashboardLayout>(reader.GetString(3), new()),
            DefaultFilters = ReadJson<DashboardDefaultFilters>(reader.GetString(4), new()),
            DefaultDateRange = ReadJson<DashboardDefaultDateRange>(reader.GetString(5), new())
        };
        try { Validate(configuration); return configuration; }
        catch (ArgumentException) { return Default(productId); }
    }

    public async Task<bool> Save(Guid productId, DashboardConfiguration configuration)
    {
        Validate(configuration);
        var widgets = DefaultWidgets();
        foreach (var (key, widget) in configuration.WidgetConfiguration) widgets[key] = widget;
        var enabled = JsonConvert.SerializeObject(configuration.EnabledModules);
        var order = JsonConvert.SerializeObject(configuration.ModuleOrder);
        var widgetJson = JsonConvert.SerializeObject(widgets);
        var layoutJson = JsonConvert.SerializeObject(configuration.DashboardLayout);
        var filtersJson = JsonConvert.SerializeObject(configuration.DefaultFilters);
        var dateJson = JsonConvert.SerializeObject(configuration.DefaultDateRange);

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO product_dashboard_configuration(product_id,enabled_modules,module_order,widget_configuration,dashboard_layout,default_filters,default_date_range,updated_at)
            SELECT @product,@enabled::jsonb,@order::jsonb,@widgets::jsonb,@layout::jsonb,@filters::jsonb,@date::jsonb,now()
            WHERE EXISTS (SELECT 1 FROM products WHERE id=@product)
            ON CONFLICT(product_id) DO UPDATE SET enabled_modules=EXCLUDED.enabled_modules,module_order=EXCLUDED.module_order,
                widget_configuration=EXCLUDED.widget_configuration,dashboard_layout=EXCLUDED.dashboard_layout,
                default_filters=EXCLUDED.default_filters,default_date_range=EXCLUDED.default_date_range,updated_at=now()
            """, connection);
        command.Parameters.AddWithValue("product", productId);
        AddJson(command, "enabled", enabled); AddJson(command, "order", order); AddJson(command, "widgets", widgetJson);
        AddJson(command, "layout", layoutJson); AddJson(command, "filters", filtersJson); AddJson(command, "date", dateJson);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public static DashboardConfiguration Default(Guid productId) => new()
    {
        ProductId = productId,
        Modules = ModuleRegistry.ToList(),
        EnabledModules = ModuleRegistry.Where(x => x.DefaultEnabled).Select(x => x.Key).ToList(),
        ModuleOrder = ModuleKeys.ToList(),
        WidgetConfiguration = DefaultWidgets(),
        DashboardLayout = new(),
        DefaultFilters = new(),
        DefaultDateRange = new()
    };

    private static Dictionary<string, DashboardWidgetConfiguration> DefaultWidgets() =>
        ModuleKeys.ToDictionary(key => key, _ => new DashboardWidgetConfiguration(), StringComparer.Ordinal);

    private static T ReadJson<T>(string json, T fallback)
    {
        try { return JsonConvert.DeserializeObject<T>(json) ?? fallback; }
        catch (JsonException) { return fallback; }
    }

    private static void AddJson(NpgsqlCommand command, string name, string value) =>
        command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Jsonb) { Value = value });

    private static void Validate(DashboardConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.EnabledModules is null || config.ModuleOrder is null || config.WidgetConfiguration is null
            || config.DashboardLayout is null || config.DefaultFilters is null || config.DefaultDateRange is null)
            throw new ArgumentException("Dashboard configuration sections cannot be null.");

        var registered = ModuleKeys.ToHashSet(StringComparer.Ordinal);
        if (config.EnabledModules.Any(key => !registered.Contains(key))
            || config.EnabledModules.Distinct(StringComparer.Ordinal).Count() != config.EnabledModules.Count)
            throw new ArgumentException("Enabled modules contain an unknown or duplicate module key.");
        if (config.ModuleOrder.Count != ModuleKeys.Length
            || config.ModuleOrder.Distinct(StringComparer.Ordinal).Count() != ModuleKeys.Length
            || !config.ModuleOrder.ToHashSet(StringComparer.Ordinal).SetEquals(registered))
            throw new ArgumentException("Module order must contain every registered module exactly once.");
        if (config.WidgetConfiguration.Keys.Any(key => !registered.Contains(key)))
            throw new ArgumentException("Widget configuration contains an unknown module key.");
        foreach (var widget in config.WidgetConfiguration.Values)
        {
            if (widget is null || widget.Size is not ("small" or "medium" or "large"))
                throw new ArgumentException("Widget size must be small, medium, or large.");
        }
        if (config.DashboardLayout.Columns is < 1 or > 3)
            throw new ArgumentException("Dashboard columns must be 1, 2, or 3.");
        foreach (var value in new[] { config.DefaultFilters.Owner, config.DefaultFilters.Status, config.DefaultFilters.Team })
            if ((value?.Trim().Length ?? 0) > 200) throw new ArgumentException("Default filter values cannot exceed 200 characters.");

        var date = config.DefaultDateRange;
        if (date.Preset is not ("7d" or "30d" or "90d" or "custom"))
            throw new ArgumentException("Date range preset must be 7d, 30d, 90d, or custom.");
        if (date.Preset == "custom" && (!date.StartDate.HasValue || !date.EndDate.HasValue || date.EndDate < date.StartDate))
            throw new ArgumentException("Custom date ranges require a start date no later than the end date.");
        if (date.Preset != "custom" && (date.StartDate.HasValue || date.EndDate.HasValue))
            throw new ArgumentException("Explicit start/end dates are only allowed with the custom preset.");
    }
}
