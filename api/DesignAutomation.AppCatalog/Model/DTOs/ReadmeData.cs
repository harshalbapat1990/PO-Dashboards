using DesignAutomation.AppCatalog.Logic;
using DesignAutomation.AppCatalog.Serializers;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>DTO that returns the structured Readme info from the API request</summary>
public record ReadmeData
{
    [JsonProperty( "id" )]
    public long ID { get; set; }

    /// <summary>Set to true if there was an exception thrown while parsing</summary>
    [JsonProperty( "error" )]
    public bool ParseError { get; set; } = true;

    /// <summary>Total length of the README for now but use for anything</summary>
    [JsonProperty( "debug" )]
    public int ContentLength { get; set; } = 0;

    /// <summary></summary>
    [JsonProperty( "isProd" )]
    public bool IsProduction { get; set; } = false;

    /// <summary>Is this repo set to private or internal</summary>
    // [JsonProperty( "isPrivate" )]
    // public bool IsPrivate { get; set; } = false;
    
    /// <summary>Name of the repo</summary>
    [JsonProperty( "repo" )]
    public string RepoName { get; set; } = String.Empty;

    /// <summary>Production Branch</summary>
    [JsonProperty( "prodBranch" )]
    public string ProductionBranch { get; set; } = String.Empty;

    /// <summary>Comon name of the project from the README</summary>
    [JsonProperty( "name" )]
    public string Name { get; set; } = String.Empty;

    /// <summary>Expected availability</summary>
    [JsonProperty( "uptime" )]
    public double Uptime { get; set; } = 0;

    /// <summary>List of project alias</summary>
    [JsonProperty( "alias" )]
    public List<string> Aliases { get; set; } = [];

    /// <summary>list of project doc links</summary>
    [JsonProperty( "docs" )]
    public List<ReadmeLink> Documents { get; set; } = [];

    /// <summary>tech stack</summary>
    [JsonProperty( "stack" )]
    public List<string> TechStack { get; set; } = [];

    /// <summary>Tier 1 API Integrations</summary>
    [JsonProperty( "apis" )]
    [JsonConverter(typeof(FlagsArrayConverter))]
    public Tier1AppType Integrations = Tier1AppType.None;

    /// <summary>Project description</summary>
    [JsonProperty( "desc" )]
    public string Description { get; set; } = String.Empty;

    /// <summary>Link to the project Management Board</summary>
    [JsonProperty( "proj" )]
    public ReadmeLink ProjectManagement { get; set; }

    /// <summary>Link to the primary web app if any</summary>
    [JsonProperty( "website" )]
    public ReadmeLink WebSite { get; set; }

    /// <summary>Other repos associated with this one</summary>
    [JsonProperty( "children" )]
    public List<ReadmeLink> ChildRepos { get; set; } = [];

    /// <summary>Team Members</summary>
    [JsonProperty( "team" )]
    public List<ReadmeMember> Team { get; set; } = [];

    /// <summary>branches</summary>
    [JsonProperty( "branches" )]
    public List<BranchCommit> Branches { get; set; } = [];

    /// <summary>GitHub HTML URL</summary>
    [JsonProperty( "url" )]
    public string URL { get; set; } = String.Empty;

    /// <summary>Some last time repo was updated but not sure yet what is corresponds to</summary>
    [JsonProperty("ts")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.MaxValue;

    /// <summary>Some completeness score</summary>
    [JsonProperty("score")]
    public int Score => ScoreCalculator.Score(this);
}

/// <summary></summary>
public struct ReadmeMember
{
    [JsonProperty( "name" )]
    public string Name { get; set; }

    [JsonProperty( "role" )]
    public string Role { get; set; }
}

/// <summary></summary>
public struct ReadmeLink
{
    [JsonProperty( "label" )]
    public string Label { get; set; }

    [JsonProperty( "url" )]
    public string URL { get; set; }
}

/// <summary>Branch Info</summary>
public struct BranchCommit
{
    public BranchCommit()
    {}

    [JsonProperty( "name" )]
    public string Name { get; set; } = String.Empty;

    [JsonProperty( "ts" )]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.MaxValue;
}