using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>Contains just the App ID the Review</summary>
public record ReviewUpdateDTO
{
    /// <summary></summary>
    [JsonProperty("id")]
    public Guid ID { get; set; }

    /// <summary>Date record last modified</summary>
    [JsonProperty("review")]
    public string Review { get; set; } = string.Empty;

}