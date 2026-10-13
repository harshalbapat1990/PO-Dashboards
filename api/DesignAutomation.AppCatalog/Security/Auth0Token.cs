using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>An Auth0 Token</summary>
/// <remarks>This is not a JWT token</remarks>
public class Auth0Token
{
    /// <summary>The ID token</summary>
    [JsonProperty("id_token")]
    public string? IDToken { get; set; }

    /// <summary>The primary access token itself</summary>
    /// <remarks>When this property is set, the JWT will be decoded to get other values</remarks>
    [JsonProperty("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>The refresh token ID to use to get a new token instead of re-logging back in</summary>
    /// <remarks>2 leg tokens will not have a refresh token</remarks>
    [JsonProperty("refresh_token")]
    public string? Refresh { get; set; }

    /// <summary>Should be Bearer</summary>
    [JsonProperty("token_type")]
    public string Type { get; set; } = "Bearer";

    /// <summary></summary>
    [JsonProperty("scope")]
    public string Scope { get; set; } = String.Empty;

    /// <summary>Number of seconds before the token expires WHEN it is fetched set by the Autodesk server.</summary>
    /// <remarks>This is never a good value to use because it has no reference. Use ExpiresOn which comes from the token itself. This may be 0 when coming from the proxy</remarks>
    [JsonProperty("expires_in")]
    public int Expires { get; set; }

    /// <summary>Date when the token will actually expire as found inside the JWT</summary>
    /// <remarks>Changing this in the file doesn't work since its pulled from the JWT itself</remarks>
    [JsonProperty("expires_on")]
    public DateTimeOffset ExpiresOn => DateTimeOffset.UtcNow.AddSeconds(this.Expires);
}