using System.Text.Json.Serialization;

namespace DesignAutomation.AppCatalog.Blazor.Models.Api;

public class APIResult<T>
{
    [JsonPropertyName("result")]
    public T? ResultObject { get; set; }

    [JsonPropertyName("resultType")]
    public APIResultType ResultCode { get; set; } = APIResultType.Unknown;

    [JsonPropertyName("debug")]
    public string? Debug { get; set; }

    [JsonIgnore]
    public bool IsSuccess => ResultCode == APIResultType.Success;
}
