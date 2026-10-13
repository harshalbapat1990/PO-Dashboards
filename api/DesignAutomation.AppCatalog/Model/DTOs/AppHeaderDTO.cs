using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>DTO Projection of the App Header</summary>
[BsonIgnoreExtraElements]
public record AppHeaderDTO
{
    /// <summary>System doc ID</summary>
    [BsonId()]
    [JsonProperty("id")]
    public Guid ID { get; set; } = Guid.Empty;

    /// <summary>Standard name of the app</summary>
    [BsonElement("name")]
    [JsonProperty("name")]
    public string FullName { get; set; } = String.Empty;

    /// <summary>Is this app conisdered in Production</summary>
    [BsonElement("isProd")]
	[JsonProperty("isProd")]
	public bool Production { get; set; }

    /// <summary>list the repo URLs</summary>
    [BsonElement("repo")]
	[JsonProperty("repo")]
	public string Repo { get; set; } = String.Empty;

    /// <summary>The production branch</summary>
    [BsonElement("branch")]
	[JsonProperty("branch")]
	public string ProductionBranch { get; set; } = String.Empty;

    /// <summary>The website home page if any</summary>
    [BsonElement("webSite")]
    [JsonProperty("webSite")]
    public string WebSite { get; set; } = String.Empty;

    /// <summary>Link to the review</summary>
    [BsonElement("review")]
    [JsonProperty("review")]
    public string ReviewURL { get; set; } = String.Empty;

    /// <summary>Returns max score of completeness</summary>
    [BsonElement("score")]
    [JsonProperty("score")]
    public int Score { get; set; } = 0;

    /// <summary>Describe the type of app</summary>
    /// <remarks>web, plugin, mobile, desktop, command line/exe (including scheduled automation), multi-client platform, APIs only, library, other</remarks>
    [BsonElement("appType")]
    [JsonProperty("appType")]
    public string AppType { get; set; } = String.Empty;

    /// <summary>Date record last modified</summary>
    [BsonElement("modified")]
    [BsonSerializer(typeof(DateTimeOffsetSerializer))]
    [JsonProperty("modified")]
    public DateTimeOffset Modified { get; set; }

    /// <summary>date created</summary>
    [BsonElement("created")]
    [BsonSerializer(typeof(DateTimeOffsetSerializer))]
    [JsonProperty("created")]
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}
