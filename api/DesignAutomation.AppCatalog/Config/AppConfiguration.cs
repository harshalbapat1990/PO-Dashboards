using DesignAutomation.AppCatalog.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DesignAutomation.AppCatalog.Config;

/// <summary>Environmenet Variables loaded from Settings</summary>
public class AppConfiguration
{
    #region [ Properties ]

    /// <summary>The enumerated environment type</summary>
    public EnvironmentType Environment { get; set; } = EnvironmentType.Local;

    /// <summary>The name of the primary database</summary>
    public string PrimaryDatabase { get; set; } = String.Empty;

    /// <summary>The connection string to the primary database</summary>
    public string PrimaryDBConnStr { get; set; } = String.Empty;

    /// <summary>The DSN for logging to Sentry</summary>
    public string SentryDSN { get; set; } = String.Empty;

    /// <summary></summary>
    public LogLevel MinLogLevel { get; set; } = LogLevel.Information;

    /// <summary>The login callback/redirect URL</summary>
    public string AuthCallback { get; set; } = String.Empty;

    /// <summary>OAuth Client Secret</summary>
    public string AuthSecret { get; set; } = String.Empty;

    /// <summary>OAuth client ID</summary>
    public string AuthClientID { get; set; } = String.Empty;

    /// <summary>OAuth Tenant endpoint URL</summary>
    public string AuthTenant { get; set; } = String.Empty;
    
    /// <summary>The OAuth Code Challenge Method</summary>
    public string CodeChallenge { get; set; } = String.Empty;

    /// <summary>Ping Code Verifier</summary>
    public string CodeVerifier { get; set; } = String.Empty;

    /// <summary>Private Key for signing reissued JWT tokens</summary>
    public string TokenKey { get; set; } = String.Empty;

    /// <summary>Private key used to sign new tokens</summary>
    public byte[] SigningKey => Convert.FromBase64String(this.TokenKey);

    /// <summary>How long do tokens last in days</summary>
    public int TokenLifespan { get; set; } = 2;

    /// <summary>Optional Password to use in Basic Auth Mode</summary>
    /// <remarks>Do not use in Production</remarks>
    public string BasicAuthPassword { get; set; } = String.Empty;

    /// <summary>the URL of the local web client</summary>
    /// <remarks>Do not use in Production</remarks>
    public string LocalClientURL { get; set; } = String.Empty;

    /// <summary>the URL of the prod web client</summary>
    public string ProductionClientURL { get; set; } = String.Empty;

    /// <summary>GitHub auth Token for the GitHub API</summary>
    public string GitHubToken { get; set; } = String.Empty;

    /// <summary>Thge topic to match on</summary>
    public string Topic { get; set; } = String.Empty;

    /// <summary>When true the client signature has to match on each call</summary>
    public bool CheckClient { get; set; } = true;

    /// <summary>The URL back to the app client</summary>
    public string ClientBaseURL(bool overrid) => this.Environment == EnvironmentType.Local || overrid ? this.LocalClientURL : this.ProductionClientURL;

    #endregion [ Properties ]
}
