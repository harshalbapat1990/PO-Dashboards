using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public record ReadmeDTO
{
    [JsonPropertyName("id")]
    public long ID { get; init; }

    [JsonPropertyName("repo")]
    public string RepoName { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string URL { get; init; } = string.Empty;

    [JsonPropertyName("ts")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.MaxValue;
}
