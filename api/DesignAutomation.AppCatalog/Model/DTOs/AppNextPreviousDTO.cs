using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>DTO for the next and previous app in the list</summary>
public record AppNextPreviousDTO
{
    public AppNextPreviousDTO() { }

    public AppNextPreviousDTO(Guid init)
    {
        this.Current = init;
        this.Previous = init;
        this.Next = init;
    }

    [JsonProperty("total")]
    public int Total { get; set; } = 0;

    [JsonProperty("cur")]
    public Guid Current { get; set; } = Guid.Empty;

    [JsonProperty("next")]
    public Guid Next { get; set; } = Guid.Empty;

    [JsonProperty("prev")]
    public Guid Previous { get; set; } = Guid.Empty;
}