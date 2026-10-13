using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>Token information sent back to the client after successul login</summary>
public class TokenResponse
{
    /// <summary>Usually the JWT encoded token string</summary>
    [JsonProperty("token")]
    public string Token { get; set; } = String.Empty;

    /// <summary>The user ID</summary>
    [JsonProperty("userID")]
    public string UserID { get; set; } = String.Empty;

    /// <summary>A refresh token if any</summary>
    [JsonProperty("refresh")]
    public string RefreshToken { get; set; } = String.Empty;

    /// <summary>Breaks out when the token will expire but should be the same as in the token</summary>
    [JsonProperty("expires")]
    public DateTimeOffset Expires { get; set; } = DateTimeOffset.MinValue;

    [JsonProperty("isNull")]
    public bool IsNull => String.IsNullOrWhiteSpace(this.Token);
}