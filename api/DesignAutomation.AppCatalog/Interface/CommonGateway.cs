using DesignAutomation.AppCatalog.Config;
using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Logic;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DesignAutomation.AppCatalog.Interface;

/// <summary></summary>
public class CommonGateway : ICommonGateway
{
    #region [ Constructor ]

    /// <summary>Constructor</summary>
    /// <param name="env">Should not really pass this as things that use settings are also set as services</param>
    /// <param name="logger">logger</param>
    /// <param name="auth"></param>
    public CommonGateway(AppConfiguration config, IAppHeaderRepository appRepo, IGitHubRepository gitHub, IUsersRepository usersRepo, ISecurityManager sec, ILogger<CommonGateway> logger)
    {
        // config
        this.Config = config;
        this.AppRepo = appRepo;
        this.UserRepo = usersRepo;
        this.GithubRepo = gitHub;
        this.Logger = logger;
        this.AuthManager = sec;
    }

    #endregion [ Constructor ]

    #region [ Properties ]

    /// <summary></summary>
    public ISecurityManager AuthManager { get; set; }

    /// <summary></summary>
    protected AppConfiguration Config { get; set; }

    /// <summary></summary>
    protected IAppHeaderRepository AppRepo { get; set; }

    /// <summary></summary>
    protected IUsersRepository UserRepo { get; set; }

    /// <summary></summary>
    protected IGitHubRepository GithubRepo { get; set; }

    /// <summary>Set the identity using the app</summary>
    public AppIdentity User { get; set; } = new AppIdentity();

    /// <summary>Capture headers for later use</summary>
    public ClientHeaders Headers { get; set; } = new ClientHeaders();

    /// <summary>The default logger</summary>
    public ILogger<CommonGateway> Logger { get; set; }

    #endregion [ Properties ]

    /// <summary>Allows adding additional headers after the class is created</summary>
    /// <param name="req">The request that if null will set to default client header values</param>
    public AppIdentity Init(HttpRequestData req)
    {
		// extract the headers
		this.Headers = req.GetClientHeaders();

        if (this.Headers == null)
        	return new();

		// try to authenticate the user if not make anonymous
		this.User = this.AuthManager.Authenticate(this.Headers.AuthToken, this.Headers.ClientKey);
		return this.User;
    }

    /// <summary></summary>
    public APIResult<PingResponse> Ping(string message)
    {
        this.Logger.LogInformation("Pinged with message '{msg}'", message );

        return new APIResult<PingResponse>(new PingResponse
        {
            Version = GetVersion(),
            Message = message.Truncate(1024),
            Environment = $"{this.Config.Environment}",
            User = this.User.Email ?? String.Empty,
            IP = Headers.ClientIP,
            Client = Headers.ClientAgent,
            APIKey = Headers.APIKey
        }, APIResultType.Success);
    }

    #region [ Apps ]

    /// <summary>Get a specific app</summary>
    public APIResult<AppHeader> GetApp(Guid appID)
    {
        if (appID == Guid.Empty)
            return new APIResult<AppHeader>(new(), APIResultType.InvalidInput);

        var app = this.AppRepo.Get(appID);

        return app == null ? new APIResult<AppHeader>(new(), APIResultType.NoMatch) : new APIResult<AppHeader>(app, APIResultType.Success);
    }

    /// <summary>Insert a new app</summary>
    public async Task<APIResult<Guid>> InsertApp(AppHeader app)
    {
        var insert = await this.AppRepo.Insert(app);
        return insert ? new APIResult<Guid>(app.ID, APIResultType.Success) : new APIResult<Guid>(Guid.Empty, APIResultType.InsertFailed);
    }

    /// <summary>Updates an app if the incoming is not stale.</summary>
    /// <param name="app"></param>
    /// <returns>New updated timestamp to send back to the client</returns>
    public async Task<APIResult<DateTimeOffset>> UpdateApp(AppHeader app)
    {
        if (app == null || app.ID == Guid.Empty)
            return new APIResult<DateTimeOffset>(DateTimeOffset.MinValue, APIResultType.InvalidInput);

        // check for a modifed date change
        var changed = await this.AppRepo.GetLastUpdate(app.ID);
        if (changed.IsValid() && changed.Modified > app.Modified)
        {
            //this.Log("Attempt to update a stale site {site}", LogLevel.Error, ("site", $"{app.ID}"));
            return new APIResult<DateTimeOffset>(DateTimeOffset.MinValue, APIResultType.UpdateStale);
        }

        var update = await this.AppRepo.Update(app);

        if (update == null )
            return new APIResult<DateTimeOffset>(DateTimeOffset.MinValue, APIResultType.UpdateFailed);

        this.Logger.LogInformation("Updated app {app}", app.ID );

        return new APIResult<DateTimeOffset>((DateTimeOffset)update, APIResultType.Success);
    }

    /// <summary>Only replaces the app review field and modified</summary>
    public async Task<APIResult<bool>> UpdateAppReview(Guid appID, string review)
    {
        if ( appID == Guid.Empty)
            return new APIResult<bool>(false, APIResultType.InvalidInput);

        var moded = await this.AppRepo.UpdateReview(appID, review.Trim());
        return moded ? new APIResult<bool>(moded, APIResultType.Success) : new APIResult<bool>(moded, APIResultType.UpdateFailed);
    }

    /// <summary>Sets last modified to right now making all outstanding udpates stale and unable to update</summary>
    public async Task<APIResult<DateTimeOffset>> UpdateAppModified(Guid appID)
    {
        var moded = await this.AppRepo.UpdateModified(appID);
        return moded == DateTimeOffset.MaxValue ? new APIResult<DateTimeOffset>(moded, APIResultType.UpdateFailed) : new APIResult<DateTimeOffset>(moded, APIResultType.Success);
    }

    /// <summary>List all the apps</summary>
    public async Task<APIResult<List<AppHeaderDTO>>> ListApps()
    {
        var results = new APIResult<List<AppHeaderDTO>>(APIResultType.Success);
        var apps = await this.AppRepo.List();
        results.ResultObject = apps.Select(item => new AppHeaderDTO
        {
            ID = item.ID,
            FullName = item.FullName,
            Production = item.Production,
            ProductionBranch = item.ProductionBranch,
            Repo = item.Repos.Count > 0 ? item.Repos.First() : String.Empty,
            WebSite = item.WebSite,
            AppType = item.AppType,
            Modified = item.Modified,
            Created = item.Created,
            ReviewURL = item.Review,
            Score = item.Score()
        }).ToList();
        return results;
    }

    /// <summary>List all the PoCs only</summary>
    public async Task<APIResult<List<PoCDTO>>> ListAppPoCs()
    {
        var results = await this.AppRepo.ProjectPoCs();
        return new APIResult<List<PoCDTO>>(results, APIResultType.Success);
    }

    /// <summary>List all the repos</summary>
    public async Task<APIResult<List<RepoDTO>>> ListAppRepos()
    {
        var results = await this.AppRepo.ProjectRepos();
        return new APIResult<List<RepoDTO>>(results, APIResultType.Success);
    }

    /// <summary>Get the Previous and Next App IDs for paging</summary>
    public async Task<APIResult<AppNextPreviousDTO>> GetAppPages(Guid appID)
    {
        var apps = await this.AppRepo.List();

        if (appID == Guid.Empty)
            return new APIResult<AppNextPreviousDTO>(new(), APIResultType.InvalidInput);

        if (apps.Count < 1)
            return new APIResult<AppNextPreviousDTO>(new AppNextPreviousDTO(), APIResultType.Success);
        else if (apps.Count == 1)
            return new APIResult<AppNextPreviousDTO>(new AppNextPreviousDTO(apps[0].ID) { Total = 1 }, APIResultType.Success);

        CircularList<Guid> list = new CircularList<Guid>(apps.Select(a => a.ID));
        if (list.Move(appID) < 0)
            list.Reset();

        var results = new AppNextPreviousDTO() { Total = apps.Count };
        list.MovePrevious();
        results.Previous = list.Current;
        list.MoveNext();
        results.Current = list.Current;
        list.MoveNext();
        results.Next = list.Current;

        return new APIResult<AppNextPreviousDTO>(results, APIResultType.Success);
    }

    /// <summary>Delete an app completely</summary>
    public async Task<APIResult<bool>> DeleteApp(Guid appID)
    {
        if (appID == Guid.Empty)
            return new APIResult<bool>(false, APIResultType.InvalidInput);

        return (await this.AppRepo.Delete(appID)) ? new APIResult<bool>(true, APIResultType.Success) : new APIResult<bool>(false, APIResultType.DeleteFailed);
    }

    /// <summary>Pull all marked READMEs and parse the data</summary>
    /// <remarks>README must be marked with gte as a topic</remarks>
    public async Task<APIResult<List<ReadmeData>>> ListRepos()
    {
        if (String.IsNullOrWhiteSpace(this.Config.Topic))
        {
            this.Logger.LogError("No topic specified in configuration");
            return new(new(), APIResultType.InvalidInput);
        }

        var results = await this.GithubRepo.ParseReposByTopic(this.Config.Topic);

        this.Logger.LogInformation("Fetched {count} repos from GitHub", results.Count);

        return new APIResult<List<ReadmeData>>(results, APIResultType.Success);
    }

    /// <summary>Get a specific repo</summary>
    public async Task<APIResult<ReadmeData>> GetRepo(long id)
    {
        if (id < 1)
        {
            this.Logger.LogError("Invalid Repo ID {id}", id);
            return new(new(), APIResultType.InvalidInput);
        }

        var results = await this.GithubRepo.ParseRepo(id);

        this.Logger.LogInformation("Fetched and parsed repo {id}", id);

        return new(results, results.ParseError ? APIResultType.NoResults : APIResultType.Success);
    }

    #endregion [ Apps ]

    #region [ Users ]

    /// <summary>Return a list of all the users</summary>
    public async Task<APIResult<List<AppUser>>> ListUsers()
    {
        this.Logger.LogInformation("Request to list all users");
        var results = new APIResult<List<AppUser>>(APIResultType.Success);
        results.ResultObject = await this.UserRepo.List();
        return results;
    }

    /// <summary>Change a users role</summary>
    public async Task<APIResult<bool>> UpdateRole(Guid userID, UserRoleLevel role)
    {
        this.Logger.LogInformation("Request to change user '{id}' to role {role}", userID, role);
        var results = new APIResult<bool>(APIResultType.Success);
        results.ResultObject = await this.UserRepo.UpdateRole(userID, role);
        return results;
    }

    #endregion [ Users ]

    #region [ Logging ]

    // /// <summary>Log a full log record on a background task</summary>
    // protected void Log(string msg, Dictionary<string, string>? args = null, LogLevel level = LogLevel.Information)
    // {
    //     if (String.IsNullOrWhiteSpace(msg))
    //         return;

    //     // log in the background
    //     Task.Run(() =>
    //     {
    //         // should always throw in the Client IP address
    //         if (args == null)
    //             args = new Dictionary<string, string>();

    //         // add default args of IP and client
    //         if (!args.ContainsKey("ip"))
    //             args.Add("ip", this.Headers.ClientIP);

    //         // add user ID
    //         if (this.User != null && !args.ContainsKey("acct"))
    //             args.Add("acct", this.User.Email ?? String.Empty);

    //         //if (!args.ContainsKey("agent"))
    //         //args.Add("agent", this.ClientAgent);

    //         string[] argAry = args.Select(a => $"{a.Key}={a.Value}").ToArray();

    //         // log it
    //         this.Logger.Log<LogMessage>(level, new EventId(), new LogMessage { Message = msg, Args = args }, null, LogMessage.FormatIt);
    //     });
    // }

    // /// <summary>Log an exception the standard logger on a background task</summary>
    // protected void Log(System.Exception exp)
    // {
    //     // log in the background
    //     Task.Run(() =>
    //     {
    //         this.Logger.LogError(exp, LogMessage.ToLogMessage(exp, true));
    //     });
    // }

    #endregion [ Logging ]

    /// <summary>Gets the version of THIS assembly</summary>
    /// <returns></returns>
    public static string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

    /// <summary>Gets the base URL of the client</summary>
    /// <returns></returns>
    public string GetClientBaseURL(EnvironmentType env)
    {
        if (env > EnvironmentType.Local)
            return this.Config.ProductionClientURL;
        else
            return this.Config.LocalClientURL;
    }

}
