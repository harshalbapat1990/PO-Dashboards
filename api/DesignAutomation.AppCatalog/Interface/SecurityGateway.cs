using DesignAutomation.AppCatalog.Config;
using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DesignAutomation.AppCatalog.Interface;

/// <summary></summary>
public interface ISecurityGateway
{
    /// <summary></summary>
    ClientHeaders Init(HttpRequestData req);

    /// <summary></summary>
    string OAuthLogin(OAuthState state);

    /// <summary></summary>
    Task<APIResult<TokenResponse>> GetToken(string code, OAuthState? state);

    /// <summary></summary>
    string OAuthLogout(string redirect);
    
    /// <summary></summary>
    APIResult<bool> Authenticate(string token, string client);
}

/// <summary></summary>
public class SecurityGateway : ISecurityGateway
{
	#region [ Constructor ]

    /// <summary>Constructor</summary>
    /// <param name="env">Should not really pass this as things that use settings are also set as services</param>
    /// <param name="logger">logger</param>
    /// <param name="auth"></param>
    public SecurityGateway(IUsersRepository usersRepo, ISecurityManager authMgr, AppConfiguration config, ILogger<SecurityGateway> logger)
    {
        this.Logger = logger;
        this.UsersRepo = usersRepo;
		this.AuthManager = authMgr;
        this.Config = config;
    }

    #endregion [ Constructor ]

	#region [ Properties ]

    /// <summary></summary>
    public ClientHeaders Headers { get; set; } = new();

    /// <summary></summary>
    public IUsersRepository UsersRepo { get; set; }

    /// <summary></summary>
    public AppConfiguration Config { get; set; }

	/// <summary></summary>
    public string ClientID => this.Config.AuthClientID;

	/// <summary></summary>
    public string ClientSecret => this.Config.AuthSecret;

	/// <summary></summary>
    public string TenantURL => this.Config.AuthTenant;

	/// <summary></summary>
    public string CallbackURL => this.Config.AuthCallback;

	/// <summary></summary>
    public ISecurityManager AuthManager { get; set; }

    /// <summary>The default logger</summary>
    public ILogger<SecurityGateway> Logger { get; set; }

    #endregion [ Properties ]

	/// <summary>Allows adding additional headers after the class is created</summary>
    /// <param name="req">The request that if null will set to default client header values</param>
    public ClientHeaders Init(HttpRequestData req)
    {
		// extract the headers
		this.Headers = req.GetClientHeaders();
		return this.Headers;
    }

    /// <summary>Generate the OAuth Login Address with optional state information and callback</summary>
    public string OAuthLogin(OAuthState state)
    {
        IOAuthManager authMgr = new PingManager(this.Config.AuthClientID, this.Config.AuthSecret, this.Config.AuthTenant, this.Config.CodeChallenge, this.Config.CodeVerifier);
        return authMgr.Login(this.Config.AuthCallback, state);
    }

    /// <summary>Callback from OAuth after login</summary>
    public async Task<APIResult<TokenResponse>> GetToken(string code, OAuthState? state)
    {
        const int expiresIn = 14;

        if (String.IsNullOrWhiteSpace(code))
            return new APIResult<TokenResponse>(new TokenResponse(), APIResultType.AuthCodeNotFound);

        IOAuthManager authMgr = new PingManager(this.Config.AuthClientID, this.Config.AuthSecret, this.Config.AuthTenant, this.Config.CodeChallenge, this.Config.CodeVerifier);

        Auth0Token? token = await authMgr.GetToken(code, this.Config.AuthCallback);

        if (token == null || String.IsNullOrWhiteSpace(token.AccessToken) )
            return new APIResult<TokenResponse>(new TokenResponse(), APIResultType.NoAccessToken);

        // now get the user info - must have user:read scopes
        IOAuthUser user = await authMgr.GetUserInfo(token.AccessToken);
        
        // make sure this user is authorized
        if (!user.Email.IsValidJacobsEmail())
            return new APIResult<TokenResponse>(new TokenResponse(), APIResultType.InvalidCredentials);

        // see if the user is in the local DB
        _ = await this.InsertUser(user);

        // HARD CODING - change to settings or DB later
        // set the role - needs a roles table later
        var role = user.Email.ToLowerInvariant().StartsWith("tag.guillory") ? UserRoleLevel.Admin : UserRoleLevel.User;

        // create a our own JWT for now to bypass roles
        JWTManager auth = new JWTManager(this.Config.TokenKey);
        TokenResponse jwt = auth.GenerateToken(new Guid(user.ID), user.Name, user.Email, this.Headers.ClientKey, expiresIn, role);

        this.Logger.LogInformation("User {user} logged in", user.Name);

        return !jwt.IsNull ? new APIResult<TokenResponse>(jwt, APIResultType.Success) : new APIResult<TokenResponse>(new TokenResponse(), APIResultType.InvalidCredentials);
    }
   
    /// <summary>OAuth Logout URL</summary>
    public string OAuthLogout(string redirect)
    {
        IOAuthManager authMgr = new PingManager(this.Config.AuthClientID, this.Config.AuthSecret, this.Config.AuthTenant, this.Config.CodeChallenge, this.Config.CodeVerifier);
        return authMgr.Logout(redirect);
    }

    /// <summary></summary>
    /// <returns></returns>
    public APIResult<bool> Authenticate(string token, string client)
    {
        var user = this.AuthManager.Authenticate(token, client);
        var results = user.IsAuthenticated && !user.IsAnonymous;

        return new(results, results ? APIResultType.Success : APIResultType.InvalidCredentials);
    }

    /// <summary>Only inserts the user if they DNE</summary>
    /// <param name="user"></param>
    /// <returns></returns>
    protected async Task<Guid> InsertUser(IOAuthUser user)
    {
        AppUser? exists = this.UsersRepo.GetByEmail(user.Email);
        if (exists != null)
            return Guid.Empty;

        // create a new user with system GUID
        AppUser acct = new AppUser()
        {
            //PingID = user.ID,
            LastLogin = DateTimeOffset.UtcNow,
            Email = user.Email,
            Role = UserRoleLevel.User
        };

        return await this.UsersRepo.Insert(acct);
    }
}