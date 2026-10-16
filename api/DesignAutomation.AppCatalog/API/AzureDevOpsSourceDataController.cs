using System.Net;
using DesignAutomation.AppCatalog.Config;
using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Interface;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.API;

/// <summary>Local fixture import and product-scoped Azure DevOps snapshot reads.</summary>
public sealed class AzureDevOpsSourceDataController(
    ICommonGateway gateway,
    IAzureDevOpsSourceDataRepository sourceData,
    IFoundationRepository foundation,
    IFoundationAccessRepository access,
    AppConfiguration configuration,
    IServiceProvider services)
{
    private readonly ILogger _logger = NullLogger.Instance;

    [Function("ListAzureDevOpsSourceData")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/foundation/products/{id:guid}/azure-devops/data")] HttpRequestData request,
        string id)
    {
        gateway.Init(request);
        if (!Guid.TryParse(id, out var productId) || !await HasProductPermission(productId))
            return request.UnauthorizedResponse();

        var query = QueryHelpers.ParseQuery(request.Url.Query);
        var projectId = query.TryGetValue("projectId", out var project) ? project.ToString() : string.Empty;
        var entityType = query.TryGetValue("entityType", out var type) ? type.ToString() : string.Empty;
        var limit = int.TryParse(query.TryGetValue("limit", out var limitValue) ? limitValue.ToString() : null, out var parsedLimit) ? parsedLimit : 100;
        var offset = int.TryParse(query.TryGetValue("offset", out var offsetValue) ? offsetValue.ToString() : null, out var parsedOffset) ? parsedOffset : 0;
        try
        {
            var page = await sourceData.List(productId, projectId, entityType, limit, offset);
            return request.OKResponse(new APIResult<AzureDevOpsSourcePage>(page, APIResultType.Success));
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to read Azure DevOps records for product {ProductId}", productId);
            return request.ReponseError(ex, _logger);
        }
    }

    [Function("ImportAzureDevOpsSnapshot")]
    public async Task<HttpResponseData> Import(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/foundation/products/{id:guid}/azure-devops/import")] HttpRequestData request,
        string id)
    {
        gateway.Init(request);
        if (!Guid.TryParse(id, out var productId) || !gateway.User.IsAuthenticated || gateway.User.Role < UserRoleLevel.Admin)
            return request.UnauthorizedResponse();
        if (configuration.Environment != EnvironmentType.Local)
            return request.CreateResponse(HttpStatusCode.NotFound);

        AzureDevOpsSnapshotImportRequest? body;
        try { body = JsonConvert.DeserializeObject<AzureDevOpsSnapshotImportRequest>(await request.ReadAsStringAsync() ?? string.Empty); }
        catch (JsonException) { return request.BadResponse("Invalid Azure DevOps snapshot JSON."); }
        if (body is null) return request.BadResponse("An Azure DevOps snapshot is required.");
        try
        {
            var result = await sourceData.Import(productId, body);
            return request.OKResponse(new APIResult<AzureDevOpsImportResult>(result, APIResultType.Success));
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to import local Azure DevOps snapshot for product {ProductId}", productId);
            return request.ReponseError(ex, _logger);
        }
    }

    [Function("SyncAzureDevOpsProject")]
    public async Task<HttpResponseData> Sync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/foundation/products/{id:guid}/azure-devops/sync")] HttpRequestData request,
        string id)
    {
        gateway.Init(request);
        if (!Guid.TryParse(id, out var productId) || !await CanManageProduct(productId))
            return request.UnauthorizedResponse();
        if (configuration.Environment == EnvironmentType.Local)
            return request.BadResponse("Live Azure DevOps sync is disabled in Local mode. Import a local snapshot instead.");
        if (string.IsNullOrWhiteSpace(configuration.AzureDevOpsAccessToken))
            return request.CreateResponse(HttpStatusCode.ServiceUnavailable);

        AzureDevOpsSyncRequest? body;
        try { body = JsonConvert.DeserializeObject<AzureDevOpsSyncRequest>(await request.ReadAsStringAsync() ?? string.Empty); }
        catch (JsonException) { return request.BadResponse("Invalid Azure DevOps sync request JSON."); }
        if (body is null || string.IsNullOrWhiteSpace(body.ProjectId))
            return request.BadResponse("projectId is required.");
        var product = (await foundation.ListProducts()).SingleOrDefault(item => item.ProductId == productId);
        if (product is null || !product.AzureProjects.Any(project => string.Equals(project.Id, body.ProjectId, StringComparison.OrdinalIgnoreCase)))
            return request.BadResponse("The Azure DevOps project ID must already be linked to this product.");
        var organization = await sourceData.GetOrganization(productId, body.ProjectId);
        if (string.IsNullOrWhiteSpace(organization))
            return request.BadResponse("Configure the Azure DevOps organization reference for this linked project before syncing.");

        var source = services.GetService<IAzureDevOpsSnapshotSource>();
        if (source is null) return request.CreateResponse(HttpStatusCode.ServiceUnavailable);
        try
        {
            var snapshot = await source.FetchProject(organization, body.ProjectId);
            var result = await sourceData.Import(productId, snapshot);
            return request.OKResponse(new APIResult<AzureDevOpsImportResult>(result, APIResultType.Success));
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure DevOps sync failed for product {ProductId} and linked project", productId);
            return request.ReponseError(ex, _logger);
        }
    }

    private async Task<bool> HasProductPermission(Guid productId)
    {
        if (!gateway.User.IsAuthenticated || gateway.User.Role < UserRoleLevel.User) return false;
        if (gateway.User.Role >= UserRoleLevel.Admin) return true;
        return Guid.TryParse(gateway.User.UserID, out var userId)
            && await access.CanReadProduct(productId, userId);
    }

    private async Task<bool> CanManageProduct(Guid productId)
    {
        if (!gateway.User.IsAuthenticated || gateway.User.Role < UserRoleLevel.User) return false;
        if (gateway.User.Role >= UserRoleLevel.Admin) return true;
        return Guid.TryParse(gateway.User.UserID, out var userId)
            && await access.CanManageProduct(productId, userId);
    }
}
