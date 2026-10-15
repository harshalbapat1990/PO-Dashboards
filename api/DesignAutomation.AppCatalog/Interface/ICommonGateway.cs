using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker.Http;

namespace DesignAutomation.AppCatalog.Interface;

/// <summary>Resolves the caller identity used by Product Health endpoints.</summary>
public interface ICommonGateway
{
    AppIdentity User { get; }
    AppIdentity Init(HttpRequestData request);
}
