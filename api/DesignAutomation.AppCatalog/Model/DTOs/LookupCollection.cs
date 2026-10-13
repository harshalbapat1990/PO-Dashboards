using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>Holds all the lookups tables in a single entity</summary>
public struct LookupCollection
{
    [JsonProperty("availability")]
    public List<EnumItem<AvailabilityLevel>> Availability { get; set; }
}