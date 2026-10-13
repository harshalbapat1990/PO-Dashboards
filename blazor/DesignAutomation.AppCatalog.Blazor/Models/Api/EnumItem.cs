using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public record EnumItem
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Description { get; init; } = string.Empty;
}
