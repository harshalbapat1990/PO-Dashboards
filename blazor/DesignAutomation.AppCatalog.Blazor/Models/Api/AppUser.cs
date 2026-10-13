using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public record AppUser
{
    [JsonPropertyName("id")]
    public Guid ID { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("lastLogin")]
    public DateTimeOffset? LastLogin { get; init; }

    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; init; }

    [JsonPropertyName("modified")]
    public DateTimeOffset Modified { get; init; }

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;
}
