using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Security;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>The database user entity</summary>
[BsonIgnoreExtraElements]
public class AppUser
{
    /// <summary></summary>
    public AppUser()
    {
        this.Created = this.Modified = DateTimeOffset.UtcNow;
    }

    /// <summary>The DB ID</summary>
    [BsonId()]
    [BsonElement("_id")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    [JsonProperty("id")]
    public Guid ID { get; set; } = Guid.NewGuid();

    [BsonElement("email")]
    [JsonProperty("email")]
    public string Email { get; set; } = String.Empty;

    /// <summary>The user's ID in Ping</summary>
    [BsonElement("p1id")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    [JsonIgnore]
    public Guid PingID { get; set; } = Guid.Empty;

    /// <summary>The date the user last login in successfully, otherwise null</summary>
    [BsonElement("lastLogin")]
    [BsonSerializer(typeof(NullableDateTimeOffsetSerializer))]
    [JsonProperty("lastLogin")]
    public DateTimeOffset? LastLogin { get; set; } = null;

    /// <summary>Date added to the database</summary>
    [BsonElement("created")]
    [BsonSerializer(typeof(DateTimeOffsetSerializer))]
    [JsonProperty("created")]
    public DateTimeOffset Created { get; set; }

    /// <summary>Date record last modified</summary>
    [BsonElement("modified")]
    [BsonSerializer(typeof(DateTimeOffsetSerializer))]
    [JsonProperty("modified")]
    public DateTimeOffset Modified { get; set; }

    /// <summary>Users role level</summary>
    [BsonElement("role")]
    [JsonProperty("role")]
    public UserRoleLevel Role { get; set; } = UserRoleLevel.User;
}
