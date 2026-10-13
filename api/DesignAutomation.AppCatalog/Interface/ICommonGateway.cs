using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker.Http;

namespace DesignAutomation.AppCatalog.Interface;

/// <summary></summary>
public interface ICommonGateway
{
    /// <summary></summary>
    AppIdentity User { get; set; }

    /// <summary></summary>
    string GetClientBaseURL(EnvironmentType env);

    AppIdentity Init(HttpRequestData req);

    /// <summary></summary>
    ClientHeaders Headers { get; set; }

    /// <summary></summary>
    APIResult<PingResponse> Ping(string message);

    /// <summary></summary>
    Task<APIResult<List<AppHeaderDTO>>> ListApps();

    /// <summary></summary>
    Task<APIResult<List<PoCDTO>>> ListAppPoCs();

    /// <summary></summary>
    Task<APIResult<List<RepoDTO>>> ListAppRepos();

    /// <summary>Get the next and previous app IDs to page to</summary>
    Task<APIResult<AppNextPreviousDTO>> GetAppPages(Guid appID);

    /// <summary></summary>
    Task<APIResult<Guid>> InsertApp(AppHeader app);

    /// <summary></summary>
    Task<APIResult<DateTimeOffset>> UpdateApp(AppHeader app);

    /// <summary></summary>
    Task<APIResult<bool>> UpdateAppReview(Guid appID, string review);

    /// <summary></summary>
    Task<APIResult<DateTimeOffset>> UpdateAppModified(Guid appID);

    /// <summary></summary>
    APIResult<AppHeader> GetApp(Guid appID);

    /// <summary></summary>
    Task<APIResult<bool>> DeleteApp(Guid appID);

    /// <summary></summary>
    Task<APIResult<List<AppUser>>> ListUsers();

    /// <summary></summary>
    Task<APIResult<bool>> UpdateRole(Guid userID, UserRoleLevel role);

    /// <summary></summary>
    Task<APIResult<List<ReadmeData>>> ListRepos();

    /// <summary></summary>
    Task<APIResult<ReadmeData>> GetRepo(long id);
}