using DesignAutomation.AppCatalog.Model;

namespace DesignAutomation.AppCatalog.Config;

/// <summary>Settings required by the Product Health API foundation.</summary>
public sealed class AppConfiguration
{
    public EnvironmentType Environment { get; set; } = EnvironmentType.Local;
    public string DatabaseProvider { get; set; } = "PostgreSql";
    public string PostgreSqlConnectionString { get; set; } = string.Empty;
    public string LocalAdminEmail { get; set; } = string.Empty;
    public string LocalAdminPassword { get; set; } = string.Empty;
    public string TokenKey { get; set; } = string.Empty;
    public bool CheckClient { get; set; } = true;
    /// <summary>Server-only GitHub token. Must not be configured in Local mode.</summary>
    public string GitHubAccessToken { get; set; } = string.Empty;
}
