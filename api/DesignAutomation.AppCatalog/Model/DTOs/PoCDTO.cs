using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>DTO Projection of the App Header</summary>
[BsonIgnoreExtraElements]
public record PoCDTO
{
    /// <summary>app ID</summary>
    [JsonProperty("appID")]
    public Guid AppID { get; set; } = Guid.Empty;

	/// <summary>Standard name of the app</summary>
    [JsonProperty("app")]
    public string App { get; set; } = String.Empty;

    /// <summary>Standard name of the app</summary>
    [JsonProperty("poc")]
    public string Contact { get; set; } = String.Empty;
}
