using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

// Maps to AppNextPreviousDTO from the API (GET /api/v1/apps/{id})
public record AppPageDTO
{
    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("cur")]
    public Guid Current { get; init; }

    [JsonPropertyName("next")]
    public Guid Next { get; init; }

    [JsonPropertyName("prev")]
    public Guid Previous { get; init; }
}
