using System.Net;
using DesignAutomation.AppCatalog.Blazor.Models.Api;

namespace DesignAutomation.AppCatalog.Blazor.Services.Api;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public APIResultType? ResultCode { get; }

    public ApiException(HttpStatusCode statusCode, string message, APIResultType? resultCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResultCode = resultCode;
    }
}
