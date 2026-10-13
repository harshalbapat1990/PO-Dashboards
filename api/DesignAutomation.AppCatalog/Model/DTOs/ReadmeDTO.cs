using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>DTO for GitHub Repo</summary>
public record ReadmeDTO
{
    [JsonProperty( "id" )]
    public long ID { get; set; }

    /// <summary>Name of the repo</summary>
    [JsonProperty( "repo" )]
    public string RepoName { get; set; } = String.Empty;

    /// <summary>GitHub HTML URL</summary>
    [JsonProperty( "url" )]
    public string URL { get; set; } = String.Empty;

    /// <summary>Some last time repo was updated but not sure yet what is corresponds to</summary>
    [JsonProperty("ts")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.MaxValue;
}
