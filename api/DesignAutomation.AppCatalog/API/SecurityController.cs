using DesignAutomation.AppCatalog.Config;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Interface;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Specialized;
using System.Net;
using System.Web;

namespace DesignAutomation.AppCatalog.API;

/// <summary></summary>
public class SecurityController
{
    public SecurityController(ISecurityGateway gate, AppConfiguration config)
    {
        this.Gateway = gate;
        this.Config = config;
    }

    #region [Properties]

    /// <summary>The concrete gateway</summary>
	private ISecurityGateway Gateway { get; set; }

	/// <summary>params in the request query</summary>
	protected NameValueCollection? Query { get; private set; }

	/// <summary></summary>
	protected ILogger Logger { get; set; } = NullLoggerProvider.Instance.CreateLogger("API");

	/// <summary>The base URL to redirect back to after login</summary>
    protected AppConfiguration Config { get; set; }

    #endregion [Properties]

	/// <summary></summary>
	/// <param name="ctx">Request context</param>
	/// <param name="role">Min role to access this method</param>
    /// <param name="category">Logging category</param>
	//protected void LogBootstrap([CallerMemberName] string caller = "", string category = "API")
	protected void Bootstrap(HttpRequestData req, FunctionContext ctx)
	{
        // setup logger
		this.Logger.LogTrace($"{ctx.FunctionDefinition.Name} triggered.");

        // save the query
        this.Query = HttpUtility.ParseQueryString(req.Url.Query);

        // add headers to everything
		this.Gateway.Init( req );
	}

    /// <summary>Standard OAuth Login</summary>
    /// <remarks>Only supports redirect at this time since Polling and Scrape are less reliable</remarks>
    [Function("OAuthLogin")]
    public HttpResponseData OAuthLogin([HttpTrigger(AuthorizationLevel.Function, "get", Route = "login")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            bool echo = this.Query.GetBoolValue("echo", false);

            // fill in the OAuthState
            OAuthState state = new OAuthState();
            state.Method = this.Query.GetEnumValue<TokenRetrieveType>("method", TokenRetrieveType.Redirect);
            state.Port = this.Query.GetIntValue("p"); // redirect port
            state.ClientID = this.Query.GetStringValue("id");
            state.LastPage = this.Query.GetStringValue("last");

            // get the full login URL
            var url = this.Gateway.OAuthLogin(state);
            this.Logger.LogTrace(url);

            // if echo is true then just echo back the URL, otherwise only redirect is supported
            return echo ? req.TextResponse(url) : req.Redirect(url);
        }
        catch (System.Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>This is the OAuth callback function after login</summary>
    [Function("Token")]
    public async Task<HttpResponseData> Token([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "token")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            // the OAuth Code
            string? code = this.Query.GetNullableStringValue("code");

            // always need a code though we could respond to tell the client something happend
            if (String.IsNullOrWhiteSpace(code))
                return req.BadResponse("Invalid input parameters.");

            // the original state data passed by the client
            string? stateStr = this.Query.GetNullableStringValue("state");

            // unravel the state param
            OAuthState? state = OAuthState.Decode(stateStr);
            EnvironmentType clientEnv = state == null ? EnvironmentType.Local : state.ClientID.GetEnumValue<EnvironmentType>();

            // now try to get the actual token
            APIResult<TokenResponse> results = await this.Gateway.GetToken(code, state);

            if (!results.IsSuccess)
                return req.Redirect($"{this.Config.ClientBaseURL(false)}/error");

            if (state == null || state.Method == TokenRetrieveType.Redirect)
                return req.Redirect($"{this.Config.ClientBaseURL(false)}/callback?jwt={results.ResultObject.Token}&last={state?.LastPage}");
            else
                return req.OKResponse(new APIResult<TokenResponse>(results.ResultObject, APIResultType.Success));
        }
        catch (System.Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

    /// <summary>Get the logout URL</summary>
    [Function("OAuthLogout")]
    public HttpResponseData OAuthLogout([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "logout")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
            // the client environment
            var clientEnv = this.Query.GetEnumValue<EnvironmentType>("id", EnvironmentType.Local);

            return req.Redirect(this.Gateway.OAuthLogout(this.Config.ClientBaseURL(false)));
        }
        catch (System.Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }

    }

    /// <summary>Only checks for an authenticated token with at least user level access</summary>
    /// <returns></returns>
    /// <remarks>Check the role level later</remarks>
    [Function("Authenticate")]
    public HttpResponseData Authenticate([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/auth")] HttpRequestData req, FunctionContext ctx)
    {
        this.Bootstrap(req, ctx);

        try
        {
		    var headers = req.GetClientHeaders();

            if (headers == null)
        	    return req.CreateResponse(HttpStatusCode.Unauthorized);

            var results = this.Gateway.Authenticate(headers.AuthToken, headers.ClientKey);

            return req.OKResponse(results);
        }
        catch (Exception exp)
        {
            return req.ReponseError(exp, this.Logger);
        }
    }

}
