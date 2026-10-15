using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker.Http;

namespace DesignAutomation.AppCatalog.Interface;

/// <summary>Validates bearer tokens for Product Health API calls.</summary>
public sealed class CommonGateway(ISecurityManager securityManager) : ICommonGateway
{
    public AppIdentity User { get; private set; } = new();

    public AppIdentity Init(HttpRequestData request)
    {
        var headers = request.GetClientHeaders();
        User = securityManager.Authenticate(headers.AuthToken, headers.ClientKey);
        return User;
    }
}
