using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public record AppHeader
{
    [JsonPropertyName("id")]
    public Guid ID { get; init; }

    [JsonPropertyName("name")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("isProd")]
    public bool Production { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; init; } = [];

    [JsonPropertyName("team")]
    public List<string> Team { get; init; } = [];

    [JsonPropertyName("repos")]
    public List<string> Repos { get; init; } = [];

    // single-repo shorthand used by AppHeaderDTO projection
    [JsonPropertyName("repo")]
    public string Repo { get; init; } = string.Empty;

    [JsonPropertyName("branch")]
    public string ProductionBranch { get; init; } = string.Empty;

    [JsonPropertyName("pm")]
    public List<string> PMLinks { get; init; } = [];

    [JsonPropertyName("docs")]
    public List<string> DocLinks { get; init; } = [];

    [JsonPropertyName("webSite")]
    public string WebSite { get; init; } = string.Empty;

    [JsonPropertyName("appType")]
    public string AppType { get; init; } = string.Empty;

    [JsonPropertyName("review")]
    public string ReviewURL { get; init; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("availability")]
    public AvailabilityLevel Availability { get; init; } = AvailabilityLevel.Undecided;

    [JsonPropertyName("modified")]
    public DateTimeOffset Modified { get; init; }

    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; init; }
}
