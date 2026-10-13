using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public record RepoDTO
{
    [JsonPropertyName("appID")]
    public Guid AppID { get; init; }

    [JsonPropertyName("app")]
    public string App { get; init; } = string.Empty;

    [JsonPropertyName("link")]
    public string Link { get; init; } = string.Empty;
}
