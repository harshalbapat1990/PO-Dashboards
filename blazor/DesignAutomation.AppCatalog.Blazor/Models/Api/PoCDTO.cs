using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public record PoCDTO
{
    [JsonPropertyName("appID")]
    public Guid AppID { get; init; }

    [JsonPropertyName("app")]
    public string App { get; init; } = string.Empty;

    [JsonPropertyName("poc")]
    public string Contact { get; init; } = string.Empty;
}
