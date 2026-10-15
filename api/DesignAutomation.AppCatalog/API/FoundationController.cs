using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Interface;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace DesignAutomation.AppCatalog.API;

/// <summary>Stage 0 organization/product setup endpoints.</summary>
public sealed class FoundationController(ICommonGateway gateway, IFoundationRepository foundation, IFoundationAccessRepository access)
{
    private readonly ILogger _logger = NullLogger.Instance;

    [Function("ListFoundationProducts")]
    public async Task<HttpResponseData> ListProducts([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/foundation/products")] HttpRequestData request, FunctionContext context)
    {
        gateway.Init(request);
        if (!gateway.User.IsAuthenticated || gateway.User.Role < UserRoleLevel.User)
            return request.UnauthorizedResponse();
        try
        {
            var products = await foundation.ListProducts();
            if (gateway.User.Role < UserRoleLevel.Admin)
            {
                if (!Guid.TryParse(gateway.User.UserID, out var userId)) return request.UnauthorizedResponse();
                var visible = new List<FoundationProductSummary>();
                foreach (var product in products)
                    if (await access.CanReadProduct(product.ProductId, userId)) visible.Add(product);
                products = visible;
            }
            return request.OKResponse(new APIResult<List<FoundationProductSummary>>(products, APIResultType.Success));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to list foundation products");
            return request.ReponseError(ex, _logger);
        }
    }

    [Function("CreateFoundationProduct")]
    public async Task<HttpResponseData> CreateProduct([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/foundation/products")] HttpRequestData request, FunctionContext context)
    {
        gateway.Init(request);
        if (!gateway.User.IsAuthenticated || gateway.User.Role < UserRoleLevel.Admin)
            return request.UnauthorizedResponse();
        if (!request.TryDeserializeJSONBody(out FoundationProductRequest? body) || body is null)
            return request.BadResponse("A valid organization, portfolio, and product payload is required.");
        try
        {
            var id = await foundation.CreateProduct(body);
            return request.OKResponse(new APIResult<Guid>(id, APIResultType.Success));
        }
        catch (ArgumentException ex)
        {
            return request.BadResponse(ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return request.CreateResponse(System.Net.HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create foundation product");
            return request.ReponseError(ex, _logger);
        }
    }

    [Function("UpdateFoundationProduct")]
    public async Task<HttpResponseData> UpdateProduct([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/foundation/products/{id:guid}")] HttpRequestData request, string id)
    {
        gateway.Init(request);
        if (!gateway.User.IsAuthenticated || !Guid.TryParse(gateway.User.UserID, out var userId) || !Guid.TryParse(id, out var productId)) return request.UnauthorizedResponse();
        if (gateway.User.Role < UserRoleLevel.Admin && !await access.CanManageProduct(productId, userId)) return request.UnauthorizedResponse();
        if (!request.TryDeserializeJSONBody(out FoundationProductRequest? body) || body is null) return request.BadResponse("A valid product payload is required.");
        try
        {
            var saved = await foundation.UpdateProduct(productId, body);
            return saved ? request.OKResponse(new APIResult<bool>(true, APIResultType.Success)) : request.CreateResponse(System.Net.HttpStatusCode.NotFound);
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) { return request.CreateResponse(System.Net.HttpStatusCode.Conflict); }
        catch (Exception ex) { _logger.LogError(ex, "Unable to update foundation product"); return request.ReponseError(ex, _logger); }
    }

    [Function("ListFoundationUsers")]
    public async Task<HttpResponseData> ListUsers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/foundation/users")] HttpRequestData request)
    {
        if (!IsAdmin(request)) return request.UnauthorizedResponse();
        try { return request.OKResponse(new APIResult<List<FoundationUserSummary>>(await access.ListUsers(), APIResultType.Success)); }
        catch (Exception ex) { _logger.LogError(ex, "Unable to list foundation users"); return request.ReponseError(ex, _logger); }
    }

    [Function("CreateFoundationUser")]
    public async Task<HttpResponseData> CreateUser([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/foundation/users")] HttpRequestData request)
    {
        if (!IsAdmin(request)) return request.UnauthorizedResponse();
        if (!request.TryDeserializeJSONBody(out FoundationUserRequest? body) || body is null) return request.BadResponse("A valid email and role are required.");
        try
        {
            if (!Enum.TryParse<UserRoleLevel>(body.Role, true, out var role) || role is not (UserRoleLevel.User or UserRoleLevel.Admin))
                return request.BadResponse("Global role must be User or Admin.");
            var id = await access.CreateUser(body.Email, role);
            return request.OKResponse(new APIResult<Guid>(id, APIResultType.Success));
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (Exception ex) { _logger.LogError(ex, "Unable to create foundation user"); return request.ReponseError(ex, _logger); }
    }

    [Function("SetFoundationUserRole")]
    public async Task<HttpResponseData> SetUserRole([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/foundation/users/{id:guid}/role")] HttpRequestData request, string id)
    {
        if (!IsAdmin(request)) return request.UnauthorizedResponse();
        if (!Guid.TryParse(id, out var userId) || !request.TryDeserializeJSONBody(out FoundationUserRequest? body) || body is null
            || !Enum.TryParse<UserRoleLevel>(body.Role, true, out var role) || role is not (UserRoleLevel.User or UserRoleLevel.Admin))
            return request.BadResponse("A valid user ID and global role (User or Admin) are required.");
        return await access.SetUserRole(userId, role) ? request.OKResponse(new APIResult<bool>(true, APIResultType.Success)) : request.CreateResponse(System.Net.HttpStatusCode.NotFound);
    }

    [Function("SetPortfolioUserAccess")]
    public async Task<HttpResponseData> SetPortfolioAccess([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/foundation/portfolios/{id:guid}/access")] HttpRequestData request, string id)
        => await SetScopedAccess(request, id, true);

    [Function("SetProductUserAccess")]
    public async Task<HttpResponseData> SetProductAccess([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/foundation/products/{id:guid}/access")] HttpRequestData request, string id)
        => await SetScopedAccess(request, id, false);

    private async Task<HttpResponseData> SetScopedAccess(HttpRequestData request, string id, bool portfolio)
    {
        if (!IsAdmin(request)) return request.UnauthorizedResponse();
        if (!Guid.TryParse(id, out var resourceId) || !request.TryDeserializeJSONBody(out FoundationAccessRequest? body) || body is null
            || !Guid.TryParse(body.UserId, out var userId)
            || !Enum.TryParse<UserRoleLevel>(body.Role, true, out var role) || role is not (UserRoleLevel.User or UserRoleLevel.ProductManager or UserRoleLevel.PortfolioManager))
            return request.BadResponse("Provide a userId and scoped role: User, ProductManager, or PortfolioManager.");
        try
        {
            var saved = portfolio ? await access.SetPortfolioAccess(resourceId, userId, role) : await access.SetProductAccess(resourceId, userId, role);
            return saved ? request.OKResponse(new APIResult<bool>(true, APIResultType.Success)) : request.CreateResponse(System.Net.HttpStatusCode.NotFound);
        }
        catch (ArgumentException ex) { return request.BadResponse(ex.Message); }
        catch (Exception ex) { _logger.LogError(ex, "Unable to set scoped user access"); return request.ReponseError(ex, _logger); }
    }

    private bool IsAdmin(HttpRequestData request)
    {
        gateway.Init(request);
        return gateway.User.IsAuthenticated && gateway.User.Role >= UserRoleLevel.Admin;
    }
}
