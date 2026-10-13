using System.Net;
using System.Text.Json;
using DesignAutomation.AppCatalog.Blazor.Models.Api;

namespace DesignAutomation.AppCatalog.Blazor.Services.Api;

public static class ApiResponseHelper
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<APIResult<T>> DeserializeAsync<T>(HttpResponseMessage response)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized  => throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized", APIResultType.Unauthorized),
            HttpStatusCode.Forbidden     => throw new ApiException(HttpStatusCode.Forbidden, "Forbidden", APIResultType.Unauthorized),
            HttpStatusCode.NotFound      => throw new ApiException(HttpStatusCode.NotFound, "Not found", APIResultType.NoMatch),
            _ when (int)response.StatusCode >= 500 => throw new ApiException(response.StatusCode, $"Server error ({(int)response.StatusCode})", APIResultType.Exception),
            _ => await ReadResultAsync<T>(response)
        };
    }

    private static async Task<APIResult<T>> ReadResultAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<APIResult<T>>(json, _options);

        return result ?? throw new ApiException(response.StatusCode, "Empty or invalid response body");
    }
}
