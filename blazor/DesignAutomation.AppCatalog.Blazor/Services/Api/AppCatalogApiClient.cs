using DesignAutomation.AppCatalog.Blazor.Models.Api;
using DesignAutomation.AppCatalog.Blazor.Models.Configuration;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace DesignAutomation.AppCatalog.Blazor.Services.Api;

public class AppCatalogApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppCatalogApiClient> _logger;

    public AppCatalogApiClient(
        HttpClient httpClient,
        IOptions<ApiOptions> options,
        ILogger<AppCatalogApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<APIResult<string>> PingAsync(string message)
    {
        var response = await _httpClient.GetAsync(
            $"api/v1/ping?msg={Uri.EscapeDataString(message)}");

        return await ApiResponseHelper.DeserializeAsync<string>(response);
    }

    public async Task<APIResult<bool>> AuthenticateAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/auth");
        return await ApiResponseHelper.DeserializeAsync<bool>(response);
    }

    public async Task<APIResult<List<ReadmeDTO>>> GetGithubReposAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/github");
        return await ApiResponseHelper.DeserializeAsync<List<ReadmeDTO>>(response);
    }

    public async Task<APIResult<ReadmeData>> GetGithubRepoAsync(long id)
    {
        var response = await _httpClient.GetAsync($"api/v1/github/{id}");
        return await ApiResponseHelper.DeserializeAsync<ReadmeData>(response);
    }

    public async Task<APIResult<List<AppHeader>>> GetAppsAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/apps");
        return await ApiResponseHelper.DeserializeAsync<List<AppHeader>>(response);
    }

    public async Task<APIResult<AppHeader>> GetAppAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"api/v1/app/{id}");
        return await ApiResponseHelper.DeserializeAsync<AppHeader>(response);
    }

    public async Task<APIResult<AppPageDTO>> GetPageAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"api/v1/apps/{id}");
        return await ApiResponseHelper.DeserializeAsync<AppPageDTO>(response);
    }

    public async Task<APIResult<List<RepoDTO>>> GetReposAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/repos");
        return await ApiResponseHelper.DeserializeAsync<List<RepoDTO>>(response);
    }

    public async Task<APIResult<List<PoCDTO>>> GetPoCsAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/pocs");
        return await ApiResponseHelper.DeserializeAsync<List<PoCDTO>>(response);
    }

    public async Task<APIResult<List<AppUser>>> GetUsersAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/users");
        return await ApiResponseHelper.DeserializeAsync<List<AppUser>>(response);
    }

    public async Task<APIResult<LookupCollection>> GetLookupsAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/menus");
        return await ApiResponseHelper.DeserializeAsync<LookupCollection>(response);
    }

    public async Task<APIResult<Guid>> InsertAppAsync(AppHeader app)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/app", app);
        return await ApiResponseHelper.DeserializeAsync<Guid>(response);
    }

    public async Task<APIResult<bool>> UpdateAppAsync(AppHeader app)
    {
        var response = await _httpClient.PutAsJsonAsync("api/v1/app", app);
        return await ApiResponseHelper.DeserializeAsync<bool>(response);
    }

    public async Task<APIResult<bool>> DeleteAppAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/app/{id}");
        var result = await ApiResponseHelper.DeserializeAsync<bool>(response);
        if (!result.IsSuccess)
            _logger.LogWarning("DeleteApp {Id} returned {Code}", id, result.ResultCode);
        return result;
    }

    public async Task<APIResult<bool>> UpdateReviewAsync(Guid id, string review)
    {
        var payload = new ReviewUpdateDTO { ID = id, Review = review };
        var response = await _httpClient.PatchAsJsonAsync("api/v1/app/review", payload);
        return await ApiResponseHelper.DeserializeAsync<bool>(response);
    }

    public async Task<APIResult<DateTimeOffset>> TouchModifiedAsync(Guid id)
    {
        var response = await _httpClient.PatchAsync($"api/v1/app/{id}", null);
        return await ApiResponseHelper.DeserializeAsync<DateTimeOffset>(response);
    }
}