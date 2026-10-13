using System.Collections.Specialized;
using System.Web;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Interface;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesignAutomation.AppCatalog.API;

/// <summary>Entry point for all APIs</summary>
public class AppCatalogController
{
    /// <summary></summary>
    public AppCatalogController(ICommonGateway gate)
    {
        this.Gateway = gate;
    }

    #region [Properties]

    /// <summary>The concrete gateway</summary>
	private ICommonGateway Gateway { get; set; }

	/// <summary>params in the request query</summary>
	protected NameValueCollection? Query { get; private set; }

    /// <summary></summary>
	protected ILogger Logger { get; set; } = NullLoggerProvider.Instance.CreateLogger("API");

    /// <summary></summary>
    protected ILogTimer Timer { get; init; } = new LogTimer();

    #endregion [Properties]

    /// <summary></summary>
	/// <param name="ctx">Request context</param>
	/// <param name="role">Min role to access this method</param>
    /// <param name="category">Logging category</param>
	//protected void LogBootstrap([CallerMemberName] string caller = "", string category = "API")
	protected void Bootstrap(HttpRequestData req, FunctionContext ctx)
	{
        // logit
		this.Logger.LogTrace($"{ctx.FunctionDefinition.Name} triggered.");

        // save the query
        this.Query = HttpUtility.ParseQueryString(req.Url.Query);

        // add headers to everything
		this.Gateway.User = this.Gateway.Init(req);
        
        this.Logger.BeginScope( new Dictionary<string,string?> {
            ["user.id"] = $"{this.Gateway.User.UserID}",
            ["user.email"] = this.Gateway.User.Email,
        });
	}

    /// <summary></summary>
    protected bool IsAuthorized(UserRoleLevel role) => this.Gateway.User.IsAuthenticated && this.Gateway.User.Role >= role;

    /// <summary>Ping the API make sure it is up and working</summary>
    [Function("Ping")]
    public HttpResponseData Ping([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/ping")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            // extract optional params
            string msg = this.Query.GetStringValue("msg");

            var results = this.Gateway.Ping(msg);

            this.Timer.Stop();
            return req.OKResponse(results);
        }
        catch (System.Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Get the specified app</summary>
    [Function("GetApp")]
    public HttpResponseData GetApp([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/app/{id}")] HttpRequestData req, string id, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            Guid appID = id.GetGuidValue();

            var results = this.Gateway.GetApp(appID);
            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>List all the apps</summary>
    [Function("ListApps")]
    public async Task<HttpResponseData> ListApps([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/apps")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            var results = await this.Gateway.ListApps();

            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>List all PoCs</summary>
    [Function("ListAppPoCs")]
    public async Task<HttpResponseData> ListAppPoCs([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/pocs")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            var results = await this.Gateway.ListAppPoCs();

            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>List all PoCs</summary>
    [Function("ListAppRepos")]
    public async Task<HttpResponseData> ListAppRepos([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/repos")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            var results = await this.Gateway.ListAppRepos();

            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Get the previous and next app page for the specified app</summary>
    [Function("GetAppPages")]
    public async Task<HttpResponseData> GetAppPages([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/apps/{id}")] HttpRequestData req, string id, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            Guid appID = id.GetGuidValue();

            var results = await this.Gateway.GetAppPages(appID);

            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Insert a new app header</summary>
    /// <returns>GUID of the inserted app</returns>
    [Function("InsertApp")]
    public async Task<HttpResponseData> InsertApp([HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/app")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            if (!req.TryDeserializeJSONBody(out AppHeader? body) || body == null)
                return req.BadResponse("No valid input!");

            var results = await this.Gateway.InsertApp(body);

            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Insert a new app header</summary>
    /// <returns>True if updated otherwise false</returns>
    [Function("UpdateApp")]
    public async Task<HttpResponseData> UpdateApp([HttpTrigger(AuthorizationLevel.Function, "put", Route = "v1/app")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            if (!req.TryDeserializeJSONBody(out AppHeader? body) || body == null)
                return req.BadResponse("No valid input!");

            var results = await this.Gateway.UpdateApp(body);

            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Update the last modified date ONLY to right now</summary>
    [Function("UpdateAppModified")]
    public async Task<HttpResponseData> UpdateAppModified([HttpTrigger(AuthorizationLevel.Function, "patch", Route = "v1/app/{id}")] HttpRequestData req, string id, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.Admin))
                return req.UnauthorizedResponse();

            Guid appID = id.GetGuidValue();

            // call the method
            var results = await this.Gateway.UpdateAppModified(appID);
            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Update the review field</summary>
    [Function("UpdateAppReview")]
    public async Task<HttpResponseData> UpdateAppReview([HttpTrigger(AuthorizationLevel.Function, "patch", Route = "v1/app/review")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.Admin))
                return req.UnauthorizedResponse();

            if (!req.TryDeserializeJSONBody(out ReviewUpdateDTO? body) || body == null)
                return req.BadResponse("No valid input!");

            // call the method
            var results = await this.Gateway.UpdateAppReview(body.ID, body.Review);
            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Delete the specified app</summary>
    [Function("DeleteApp")]
    public async Task<HttpResponseData> DeleteApp([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "v1/app/{id}")] HttpRequestData req, string id, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.Admin))
                return req.UnauthorizedResponse();

            Guid appID = id.GetGuidValue();

            // call the method
            var results = await this.Gateway.DeleteApp(appID);
            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>List all existing users</summary>
    [Function("ListUsers")]
    public async Task<HttpResponseData> ListUsers([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/users")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.Admin))
                return req.UnauthorizedResponse();

            // call the method
            var results = await this.Gateway.ListUsers();

            return req.OKResponse(results);
        }
        catch (System.Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Gets all the lookup values in one call</summary>
    [Function("GetLookups")]
    public HttpResponseData GetLookups([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/menus")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            APIResult<LookupCollection> results = new APIResult<LookupCollection> (APIResultType.Success);

            LookupCollection tables = new LookupCollection();
            tables.Availability = Enum.GetValues(typeof(AvailabilityLevel)).Cast<AvailabilityLevel>().Select((item,idx) => new EnumItem<AvailabilityLevel>(item)).ToList();
            results.ResultObject  = tables;

            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Get GitHub Readme</summary>
    [Function("GetGitHubRepo")]
    public async Task<HttpResponseData> GetGitHubRepo([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/github/{id}")] HttpRequestData req, string id, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            if ( !Int64.TryParse(id, out long repoID) )
                return req.BadResponse("Invalid input!");

            var results = await this.Gateway.GetRepo(repoID);
            this.Timer.Stop();
            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>List GitHub Readmes</summary>
    [Function("ListGitHubRepos")]
    public async Task<HttpResponseData> ListGitHubRepos([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/github")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            if (!this.IsAuthorized(UserRoleLevel.User))
                return req.UnauthorizedResponse();

            var results = await this.Gateway.ListRepos();
            this.Timer.Stop();
            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

}
