using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>A response back from the ping service</summary>
public class PingResponse
{
    [JsonProperty( "env" )]
    public string Environment { get; set; } = "Unknown";

    [JsonProperty( "ver" )]
    public string Version { get; set; } = String.Empty;

    [JsonProperty( "msg" )]
    public string Message { get; set; } = String.Empty;

    [JsonProperty("db", NullValueHandling=NullValueHandling.Ignore)]
    public string? Database { get; set; } = null;

    [JsonProperty("user")]
    public string User { get; set; } = String.Empty;

    /// <summary>Client Agent</summary>
    [JsonProperty("client")]
    public string Client { get; set; } = String.Empty;

    [JsonProperty("ip")]
    public string IP { get; set; } = "0.0.0.0";
    
    [JsonIgnore]
    public string APIKey { get; set; } = String.Empty;

    [JsonProperty("ts")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
