using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>DTO Projection of the App Header</summary>
[BsonIgnoreExtraElements]
public record RepoDTO
{
    /// <summary>app ID</summary>
    [JsonProperty("appID")]
    public Guid AppID { get; set; } = Guid.Empty;

	/// <summary>Standard name of the app</summary>
    [JsonProperty("app")]
    public string App { get; set; } = String.Empty;

    /// <summary>Repo Link</summary>
    [JsonProperty("link")]
    public string Link { get; set; } = String.Empty;
}
