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
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.API;

/// <summary>
/// Persists operator supplied GitHub snapshots and exposes product scoped reads.
/// Local requests never call GitHub; this endpoint is deliberately an import API.
/// </summary>
public sealed class GitHubSourceDataController(
    ICommonGateway gateway,
    IGitHubSourceDataRepository sourceData,
    IFoundationAccessRepository access,
    AppConfiguration configuration)
{
    private readonly ILogger _logger = NullLogger.Instance;

    [Function("ListGitHubSourceData")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/foundation/products/{id:guid}/github/data")] HttpRequestData request,
        string id)
    {
        gateway.Init(request);
        if (!Guid.TryParse(id, out var productId) || !await HasProductPermission(productId))
            return request.UnauthorizedResponse();

        var query = QueryHelpers.ParseQuery(request.Url.Query);
        var repositoryUrl = query.TryGetValue("repositoryUrl", out var url) ? url.ToString() : string.Empty;
        var entityType = query.TryGetValue("entityType", out var type) ? type.ToString() : string.Empty;
        var limit = int.TryParse(query.TryGetValue("limit", out var limitValue) ? limitValue.ToString() : null, out var parsedLimit) ? parsedLimit : 100;
        var offset = int.TryParse(query.TryGetValue("offset", out var offsetValue) ? offsetValue.ToString() : null, out var parsedOffset) ? parsedOffset : 0;
        try
        {
            var page = await sourceData.List(productId, repositoryUrl, entityType, limit, offset);
            return request.OKResponse(new APIResult<GitHubSourcePage>(page, APIResultType.Success));
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to read stored GitHub records for product {ProductId}", productId);
            return request.ReponseError(ex, _logger);
        }
    }

    [Function("ImportGitHubSnapshot")]
    public async Task<HttpResponseData> Import(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/foundation/products/{id:guid}/github/import")] HttpRequestData request,
        string id)
    {
        gateway.Init(request);
        if (!Guid.TryParse(id, out var productId) || !gateway.User.IsAuthenticated || gateway.User.Role < UserRoleLevel.Admin)
            return request.UnauthorizedResponse();
        if (configuration.Environment != EnvironmentType.Local)
            return request.CreateResponse(HttpStatusCode.NotFound);

        GitHubSnapshotImportRequest? body;
        try { body = JsonConvert.DeserializeObject<GitHubSnapshotImportRequest>(await request.ReadAsStringAsync() ?? string.Empty); }
        catch (JsonException) { return request.BadResponse("Invalid GitHub snapshot JSON."); }
        if (body is null) return request.BadResponse("A GitHub snapshot is required.");
        try
        {
            var count = await sourceData.Import(productId, body.RepositoryUrl, body.Records);
            return request.OKResponse(new APIResult<GitHubImportResult>(
                new GitHubImportResult(body.RepositoryUrl, body.Records.Count, count), APIResultType.Success));
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to import local GitHub snapshot for product {ProductId}", productId);
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
}
