using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Model;

namespace DesignAutomation.AppCatalog.Config;

/// <summary>Settings for using GitHub API</summary>
public class GithubAPIConfiguration
{
    #region [ Properties ]

    /// <summary>The enumerated environment type</summary>
    public EnvironmentType Environment { get; set; } = EnvironmentType.Local;

    /// <summary>GitHub auth Token for the GitHub API</summary>
    public string GitHubToken { get; set; } = String.Empty;

    /// <summary>The topic to match on</summary>
    public string Topic { get; set; } = String.Empty;

    /// <summary>The GitHub Client name to use</summary>
    public string ClientName { get; set; } = "people-places-solutions";

    /// <summary>Are the settings valid</summary>
    public bool IsValid() => !new string[] {this.Topic, this.GitHubToken}.IsAnyNullOrWhiteSpace();

    #endregion [ Properties ]
}
