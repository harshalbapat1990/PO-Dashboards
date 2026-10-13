using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public record LookupCollection
{
    [JsonPropertyName("availability")]
    public List<EnumItem> Availability { get; init; } = [];
}
