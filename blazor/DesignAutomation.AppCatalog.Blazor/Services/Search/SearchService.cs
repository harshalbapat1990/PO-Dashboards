using DesignAutomation.AppCatalog.Blazor.Services.Api;
using Microsoft.Extensions.Logging;

namespace DesignAutomation.AppCatalog.Blazor.Services.Search;

public class SearchService
{
    private readonly AppCatalogApiClient _apiClient;
    private readonly ILogger<SearchService> _logger;

    public SearchService(AppCatalogApiClient apiClient, ILogger<SearchService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return [];

        var results = new List<SearchResult>();

        try
        {
            var appsTask  = _apiClient.GetAppsAsync();
            var reposTask = _apiClient.GetGithubReposAsync();
            var pocsTask  = _apiClient.GetPoCsAsync();
            var usersTask = _apiClient.GetUsersAsync();

            await Task.WhenAll(appsTask, reposTask, pocsTask, usersTask);

            var apps  = (await appsTask).ResultObject ?? [];
            var repos = (await reposTask).ResultObject ?? [];
            var pocs  = (await pocsTask).ResultObject ?? [];
            var users = (await usersTask).ResultObject ?? [];

            results.AddRange(apps
                .Where(a => a.FullName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(a => new SearchResult(SearchResultType.Application, a.FullName, a.AppType, $"/app/{a.ID}")));

            results.AddRange(repos
                .Where(r => r.RepoName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(r => new SearchResult(SearchResultType.Repository, r.RepoName, null, $"/github/{r.ID}")));

            results.AddRange(pocs
                .Where(p => p.App.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            p.Contact.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(p => new SearchResult(SearchResultType.PoC, p.App, p.Contact, $"/app/{p.AppID}")));

            results.AddRange(users
                .Where(u => u.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(u => new SearchResult(SearchResultType.User, u.Email, u.Role, "/users")));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Global search failed for term '{Term}'.", term);
        }

        return results;
    }
}
