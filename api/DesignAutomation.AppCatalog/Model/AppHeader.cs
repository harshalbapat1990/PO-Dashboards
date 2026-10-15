using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>
/// AppHeader is the central business object for holding metadata on an app
/// </summary>
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
    [JsonProperty("id")]
    public Guid ID { get; set; }

    /// <summary>Standard name of the app</summary>
    [JsonProperty("name")]
    public string FullName { get; set; } = String.Empty;

    /// <summary>Any additional alias names it might be known by</summary>
    [JsonProperty("aliases")]
    public List<string> Aliases
    {
        set => this._aliases = value.Select(s => s.Trim()).ToList();
        get => this._aliases;
    }

    /// <summary>Is this app conisdered in Production</summary>
    [JsonProperty("isProd")]
    public bool Production { get; set; }

    /// <summary>List the people on the team</summary>
    [JsonProperty("team")]
    public List<string> Team
    {
        set => this._team = value.Select(s => s.Trim()).ToList();
        get => this._team;
    }

    /// <summary>Describe the app</summary>
    [JsonProperty("description")]
    public string Description { get; set; } = String.Empty;

    /// <summary>list the repo URLs</summary>
    [JsonProperty("repos")]
    public List<string> Repos
    {
        set => this._repos = value.Select(s => s.Trim()).ToList();
        get => this._repos;
    }

    /// <summary>The production branch</summary>
    [JsonProperty("branch")]
    public string ProductionBranch { get; set; } = String.Empty;

    /// <summary>list of any project management dashboards</summary>
    [JsonProperty("pm")]
    public List<string> PMLinks
    {
        set => this._pm = value.Select(s => s.Trim()).ToList();
        get => this._pm;
    }

    /// <summary>list of any additional docs links</summary>
    [JsonProperty("docs")]
    public List<string> DocLinks
    {
        set => this._docs = value.Select(s => s.Trim()).ToList();
        get => this._docs;
    }

    /// <summary>The website home page if any</summary>
    [JsonProperty("webSite")]
    public string WebSite { get; set; } = String.Empty;

    /// <summary>Desibe the hosting setup</summary>
    [JsonProperty("host")]
    public string Hosting { get; set; } = String.Empty;

    /// <summary>Describe the type of app</summary>
    /// <remarks>web, plugin, mobile, desktop, command line/exe (including scheduled automation), multi-client platform, APIs only, library, other</remarks>
    [JsonProperty("appType")]
    public string AppType { get; set; } = String.Empty;

    /// <summary>Describe the tech stack</summary>
    [JsonProperty("stack")]
    public string TechStack { get; set; } = String.Empty;

    /// <summary>Pipeline Environments</summary>
    [JsonProperty("env")]
    public string Environments { get; set; } = String.Empty;

    /// <summary>Describe the IDM Provider</summary>
    [JsonProperty("idm")]
    public string IdentityManagement { get; set; } = String.Empty;

    /// <summary>Describe the external data integrations</summary>
    [JsonProperty("integrations")]
    public string Integrations { get; set; } = String.Empty;

    /// <summary>Describe the current scale</summary>
    [JsonProperty("scale")]
    public string Scale { get; set; } = String.Empty;

    /// <summary>Describe the projected growth</summary>
    [JsonProperty("growth")]
    public string Growth { get; set; } = String.Empty;

    /// <summary>Additional comments</summary>
    [JsonProperty("comments")]
    public string Comments { get; set; } = String.Empty;

    /// <summary>The worst case unavailability of the app per incident</summary>
    [JsonProperty("availability")]
    public AvailabilityLevel Availability { get; set; } = AvailabilityLevel.Undecided;

    /// <summary>Link to the Loop Checklist and Review documentation</summary>
    [JsonProperty("review")]
    public string Review { get; set; } = String.Empty;

    /// <summary>Date record last modified</summary>
    [JsonProperty("modified")]
    public DateTimeOffset Modified { get; set; }

    /// <summary>date created</summary>
    [JsonProperty("created")]
    public DateTimeOffset Created { get; set; }
}
