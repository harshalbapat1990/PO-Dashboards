using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public record ReadmeData
{
    [JsonPropertyName("id")]
    public long ID { get; init; }

    [JsonPropertyName("error")]
    public bool ParseError { get; init; } = true;

    [JsonPropertyName("debug")]
    public int ContentLength { get; init; }

    [JsonPropertyName("isProd")]
    public bool IsProduction { get; init; }

    [JsonPropertyName("repo")]
    public string RepoName { get; init; } = string.Empty;

    [JsonPropertyName("prodBranch")]
    public string ProductionBranch { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("uptime")]
    public double Uptime { get; init; }

    [JsonPropertyName("alias")]
    public List<string> Aliases { get; init; } = [];

    [JsonPropertyName("docs")]
    public List<ReadmeLink> Documents { get; init; } = [];

    [JsonPropertyName("stack")]
    public List<string> TechStack { get; init; } = [];

    // API serializes Tier1AppType flags enum as a string array
    [JsonPropertyName("apis")]
    public List<string> Integrations { get; init; } = [];

    [JsonPropertyName("desc")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("proj")]
    public ReadmeLink? ProjectManagement { get; init; }

    [JsonPropertyName("website")]
    public ReadmeLink? WebSite { get; init; }

    [JsonPropertyName("children")]
    public List<ReadmeLink> ChildRepos { get; init; } = [];

    [JsonPropertyName("team")]
    public List<ReadmeMember> Team { get; init; } = [];

    [JsonPropertyName("branches")]
    public List<BranchCommit> Branches { get; init; } = [];

    [JsonPropertyName("url")]
    public string URL { get; init; } = string.Empty;

    [JsonPropertyName("ts")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.MaxValue;

    [JsonPropertyName("score")]
    public int Score { get; init; }
}

public record ReadmeMember
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;
}

public record ReadmeLink
{
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string URL { get; init; } = string.Empty;
}

public record BranchCommit
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("ts")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.MaxValue;
}
