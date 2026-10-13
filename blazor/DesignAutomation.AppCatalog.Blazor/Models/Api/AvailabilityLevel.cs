using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AvailabilityLevel : byte
{
    Undecided = 0,
    OneWeek = 1,
    OneDay = 2,
    HalfDay = 3,
    OneHour = 4,
    OneMinute = 5
}
