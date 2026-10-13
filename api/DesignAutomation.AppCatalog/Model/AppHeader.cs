using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>
/// AppHeader is the central business object for holding metadata on an app
/// </summary>
[BsonIgnoreExtraElements]
public record AppHeader
{
    private List<string> _aliases = [];
    private List<string> _team = [];
    private List<string> _repos = [];
    private List<string> _pm = [];
    private List<string> _docs = [];

    /// <summary></summary>
    public AppHeader()
    {
        this.ID = Guid.NewGuid();
        this.Created = this.Modified = DateTimeOffset.UtcNow;
    }

    /// <summary>System doc ID</summary>
    [BsonId()]
    [BsonElement("_id")]
    [JsonProperty("id")]
    public Guid ID { get; set; }

    /// <summary>Standard name of the app</summary>
    [BsonElement("name")]
    [JsonProperty("name")]
    public string FullName { get; set; } = String.Empty;

    /// <summary>Any additional alias names it might be known by</summary>
    [BsonElement("aliases")]
    [JsonProperty("aliases")]
    public List<string> Aliases
    {
        set => this._aliases = value.Select(s => s.Trim()).ToList();
        get => this._aliases;
    }

    /// <summary>Is this app conisdered in Production</summary>
    [BsonElement("isProd")]
    [JsonProperty("isProd")]
    public bool Production { get; set; }

    /// <summary>List the people on the team</summary>
    [BsonElement("team")]
    [JsonProperty("team")]
    public List<string> Team
    {
        set => this._team = value.Select(s => s.Trim()).ToList();
        get => this._team;
    }

    /// <summary>Describe the app</summary>
    [BsonElement("description")]
    [JsonProperty("description")]
    public string Description { get; set; } = String.Empty;

    /// <summary>list the repo URLs</summary>
    [BsonElement("repos")]
    [JsonProperty("repos")]
    public List<string> Repos
    {
        set => this._repos = value.Select(s => s.Trim()).ToList();
        get => this._repos;
    }

    /// <summary>The production branch</summary>
    [BsonElement("branch")]
    [JsonProperty("branch")]
    public string ProductionBranch { get; set; } = String.Empty;

    /// <summary>list of any project management dashboards</summary>
    [BsonElement("pm")]
    [JsonProperty("pm")]
    public List<string> PMLinks
    {
        set => this._pm = value.Select(s => s.Trim()).ToList();
        get => this._pm;
    }

    /// <summary>list of any additional docs links</summary>
    [BsonElement("docs")]
    [JsonProperty("docs")]
    public List<string> DocLinks
    {
        set => this._docs = value.Select(s => s.Trim()).ToList();
        get => this._docs;
    }

    /// <summary>The website home page if any</summary>
    [BsonElement("webSite")]
    [JsonProperty("webSite")]
    public string WebSite { get; set; } = String.Empty;

    /// <summary>Desibe the hosting setup</summary>
    [BsonElement("host")]
    [JsonProperty("host")]
    public string Hosting { get; set; } = String.Empty;

    /// <summary>Describe the type of app</summary>
    /// <remarks>web, plugin, mobile, desktop, command line/exe (including scheduled automation), multi-client platform, APIs only, library, other</remarks>
    [BsonElement("appType")]
    [JsonProperty("appType")]
    public string AppType { get; set; } = String.Empty;

    /// <summary>Describe the tech stack</summary>
    [BsonElement("stack")]
    [JsonProperty("stack")]
    public string TechStack { get; set; } = String.Empty;

    /// <summary>Pipeline Environments</summary>
    [BsonElement("env")]
    [JsonProperty("env")]
    public string Environments { get; set; } = String.Empty;

    /// <summary>Describe the IDM Provider</summary>
    [BsonElement("idm")]
    [JsonProperty("idm")]
    public string IdentityManagement { get; set; } = String.Empty;

    /// <summary>Describe the external data integrations</summary>
    [BsonElement("integrations")]
    [JsonProperty("integrations")]
    public string Integrations { get; set; } = String.Empty;

    /// <summary>Describe the current scale</summary>
    [BsonElement("scale")]
    [JsonProperty("scale")]
    public string Scale { get; set; } = String.Empty;

    /// <summary>Describe the projected growth</summary>
    [BsonElement("growth")]
    [JsonProperty("growth")]
    public string Growth { get; set; } = String.Empty;

    /// <summary>Additional comments</summary>
    [BsonElement("comments")]
    [JsonProperty("comments")]
    public string Comments { get; set; } = String.Empty;

    /// <summary>The worst case unavailability of the app per incident</summary>
    [BsonElement("availability")]
    [JsonProperty("availability")]
    public AvailabilityLevel Availability { get; set; } = AvailabilityLevel.Undecided;

    /// <summary>Link to the Loop Checklist and Review documentation</summary>
    [BsonElement("review")]
    [JsonProperty("review")]
    public string Review { get; set; } = String.Empty;

    /// <summary>Date record last modified</summary>
    [BsonElement("modified")]
    [BsonSerializer(typeof(DateTimeOffsetSerializer))]
    [JsonProperty("modified")]
    public DateTimeOffset Modified { get; set; }

    /// <summary>date created</summary>
    [BsonElement("created")]
    [BsonSerializer(typeof(DateTimeOffsetSerializer))]
    [JsonProperty("created")]
    public DateTimeOffset Created { get; set; }
}
