using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public record ReviewUpdateDTO
{
    [JsonPropertyName("id")]
    public Guid ID { get; init; }

    [JsonPropertyName("review")]
    public string Review { get; init; } = string.Empty;
}
