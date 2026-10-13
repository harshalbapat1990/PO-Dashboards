using DesignAutomation.AppCatalog.Extensions;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>Encapsulates the Client Headers we need later</summary>
public record ClientHeaders
{
    public ClientHeaders()
    {
        this.ClientIP = "0.0.0.0";
    }

    #region [ Properties ]

    /// <summary>Store the client agent</summary>
    public string ClientAgent { get; set; } = "no client";

    /// <summary>Store the Client IP from the Request</summary>
    public string ClientIP
    {
        get;
        set => field = !String.IsNullOrWhiteSpace( value ) ? value.Trim() : "0.0.0.0";
    }

    /// <summary>A hashed signature of the browser</summary>
    /// <remarks>Needs to use HMAC SHA to encrypt properly</remarks>
    public string ClientKey => $"{this.ClientIP}|{this.ClientAgent}".HashToSHA256().ToHexString();

    /// <summary>Stores a Bearer JWT Auth Token from the header</summary>
    public string AuthToken { get; set; } = String.Empty;

    /// <summary></summary>
    public DateTimeOffset RequestTimestamp { get; set; }

    /// <summary></summary>
    public string APIKey { get; set; } = String.Empty;

    /// <summary>Stores any CSRF Token from the header</summary>
    public string CSRFToken { get; set; } = String.Empty;

    #endregion [ Properties ]
}
