using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>DTO Projection of the App Header</summary>
public record AppHeaderDTO
{
    /// <summary>System doc ID</summary>
    [JsonProperty("id")]
    public Guid ID { get; set; } = Guid.Empty;

    /// <summary>Standard name of the app</summary>
    [JsonProperty("name")]
    public string FullName { get; set; } = String.Empty;

    /// <summary>Is this app conisdered in Production</summary>
	[JsonProperty("isProd")]
	public bool Production { get; set; }

    /// <summary>list the repo URLs</summary>
	[JsonProperty("repo")]
	public string Repo { get; set; } = String.Empty;

    /// <summary>The production branch</summary>
	[JsonProperty("branch")]
	public string ProductionBranch { get; set; } = String.Empty;

    /// <summary>The website home page if any</summary>
    [JsonProperty("webSite")]
    public string WebSite { get; set; } = String.Empty;

    /// <summary>Link to the review</summary>
    [JsonProperty("review")]
    public string ReviewURL { get; set; } = String.Empty;

    /// <summary>Returns max score of completeness</summary>
    [JsonProperty("score")]
    public int Score { get; set; } = 0;

    /// <summary>Describe the type of app</summary>
    /// <remarks>web, plugin, mobile, desktop, command line/exe (including scheduled automation), multi-client platform, APIs only, library, other</remarks>
    [JsonProperty("appType")]
    public string AppType { get; set; } = String.Empty;

    /// <summary>Date record last modified</summary>
    [JsonProperty("modified")]
    public DateTimeOffset Modified { get; set; }

    /// <summary>date created</summary>
    [JsonProperty("created")]
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}
