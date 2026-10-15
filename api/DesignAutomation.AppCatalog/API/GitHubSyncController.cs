using System.Net;
using DesignAutomation.AppCatalog.Config;
using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Interface;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.API;

/// <summary>Explicit, authorized GitHub sync. No sync source is registered in Local mode.</summary>
public sealed class GitHubSyncController(
    ICommonGateway gateway,
    IFoundationRepository foundation,
    IFoundationAccessRepository access,
    IGitHubSourceDataRepository sourceData,
    IServiceProvider services,
    AppConfiguration configuration)
{
    private readonly ILogger _logger = NullLogger.Instance;

    [Function("SyncGitHubRepository")]
    public async Task<HttpResponseData> Sync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/foundation/products/{id:guid}/github/sync")] HttpRequestData request,
        string id)
    {
        gateway.Init(request);
        if (!Guid.TryParse(id, out var productId) || !await CanManage(productId))
            return request.UnauthorizedResponse();
        if (configuration.Environment == EnvironmentType.Local)
            return request.BadResponse("Live GitHub sync is disabled in Local mode. Import a local snapshot instead.");
        if (string.IsNullOrWhiteSpace(configuration.GitHubAccessToken))
            return request.CreateResponse(HttpStatusCode.ServiceUnavailable);

        GitHubSyncRequest? body;
        try { body = JsonConvert.DeserializeObject<GitHubSyncRequest>(await request.ReadAsStringAsync() ?? string.Empty); }
        catch (JsonException) { return request.BadResponse("Invalid GitHub sync request JSON."); }
        if (body is null || string.IsNullOrWhiteSpace(body.RepositoryUrl))
            return request.BadResponse("repositoryUrl is required.");

        var product = (await foundation.ListProducts()).SingleOrDefault(item => item.ProductId == productId);
        if (product is null || !product.GitHubRepositories.Contains(body.RepositoryUrl, StringComparer.OrdinalIgnoreCase))
            return request.BadResponse("The repository URL must already be linked to this product.");

        var source = services.GetService<IGitHubSnapshotSource>();
        if (source is null) return request.CreateResponse(HttpStatusCode.ServiceUnavailable);
        try
        {
            var snapshot = await source.FetchRepository(body.RepositoryUrl);
            var count = 0;
            foreach (var batch in snapshot.Records.Chunk(5000))
                count += await sourceData.Import(productId, snapshot.RepositoryUrl, batch);
            return request.OKResponse(new APIResult<GitHubImportResult>(
                new GitHubImportResult(snapshot.RepositoryUrl, snapshot.Records.Count, count), APIResultType.Success));
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub sync failed for product {ProductId} and linked repository", productId);
            return request.ReponseError(ex, _logger);
        }
    }

    private async Task<bool> CanManage(Guid productId)
    {
        if (!gateway.User.IsAuthenticated || gateway.User.Role < UserRoleLevel.User) return false;
        if (gateway.User.Role >= UserRoleLevel.Admin) return true;
        return Guid.TryParse(gateway.User.UserID, out var userId)
            && await access.CanManageProduct(productId, userId);
    }
}
