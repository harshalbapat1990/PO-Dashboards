using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>Contains just the Site ID and Last Modified Date</summary>
public record UpdatedDTO
{
    /// <summary></summary>
    [JsonProperty("id")]
    public Guid ID { get; set; }

    /// <summary>Date record last modified</summary>
    [JsonProperty("modified")]
    public DateTimeOffset Modified { get; set; } = DateTimeOffset.MinValue;

    public bool IsValid() => this.Modified > DateTimeOffset.MinValue && this.ID != Guid.Empty;
}