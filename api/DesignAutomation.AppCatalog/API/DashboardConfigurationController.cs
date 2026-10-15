using System.Net;
using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Interface;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.API;

/// <summary>Reads and persists each product's Stage 2 dashboard configuration.</summary>
public sealed class DashboardConfigurationController(
    ICommonGateway gateway,
    IDashboardConfigurationRepository configurations,
    IFoundationAccessRepository access)
{
    private readonly ILogger _logger = NullLogger.Instance;

    [Function("GetDashboardConfiguration")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/foundation/products/{id:guid}/dashboard-configuration")] HttpRequestData request,
        string id)
    {
        gateway.Init(request);
        if (!Guid.TryParse(id, out var productId) || !await HasProductPermission(productId, write: false))
            return request.UnauthorizedResponse();
        try
        {
            var configuration = await configurations.Get(productId);
            return configuration is null
                ? request.CreateResponse(HttpStatusCode.NotFound)
                : request.OKResponse(new APIResult<DashboardConfiguration>(configuration, APIResultType.Success));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load dashboard configuration for product {ProductId}", productId);
            return request.ReponseError(ex, _logger);
        }
    }

    [Function("SaveDashboardConfiguration")]
    public async Task<HttpResponseData> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/foundation/products/{id:guid}/dashboard-configuration")] HttpRequestData request,
        string id)
    {
        gateway.Init(request);
        if (!Guid.TryParse(id, out var productId) || !await HasProductPermission(productId, write: true))
            return request.UnauthorizedResponse();

        DashboardConfiguration? body;
        try { body = JsonConvert.DeserializeObject<DashboardConfiguration>(await request.ReadAsStringAsync() ?? string.Empty); }
        catch (JsonException) { return request.BadResponse("Invalid dashboard configuration JSON."); }
        if (body is null) return request.BadResponse("Dashboard configuration is required.");
        if (body.ProductId != Guid.Empty && body.ProductId != productId)
            return request.BadResponse("The configuration productId must match the route product ID.");
        try
        {
            if (!await configurations.Save(productId, body)) return request.CreateResponse(HttpStatusCode.NotFound);
            var saved = await configurations.Get(productId);
            return request.OKResponse(new APIResult<DashboardConfiguration>(saved!, APIResultType.Success));
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to save dashboard configuration for product {ProductId}", productId);
            return request.ReponseError(ex, _logger);
        }
    }

    private async Task<bool> HasProductPermission(Guid productId, bool write)
    {
        if (!gateway.User.IsAuthenticated || gateway.User.Role < UserRoleLevel.User) return false;
        if (gateway.User.Role >= UserRoleLevel.Admin) return true;
        if (!Guid.TryParse(gateway.User.UserID, out var userId)) return false;
        return write
            ? await access.CanManageProduct(productId, userId)
            : await access.CanReadProduct(productId, userId);
    }
}
