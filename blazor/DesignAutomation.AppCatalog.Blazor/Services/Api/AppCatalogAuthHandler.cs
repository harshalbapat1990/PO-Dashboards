using System.Net.Http.Headers;
using DesignAutomation.AppCatalog.Blazor.Models.Configuration;
using DesignAutomation.AppCatalog.Blazor.Services.Storage;
using Microsoft.Extensions.Options;

namespace DesignAutomation.AppCatalog.Blazor.Services.Api;

public class AppCatalogAuthHandler : DelegatingHandler
{
    private readonly ITokenStorage _tokenStorage;
    private readonly ApiOptions _options;

    public AppCatalogAuthHandler(
        ITokenStorage tokenStorage,
        IOptions<ApiOptions> options)
    {
        _tokenStorage = tokenStorage;
        _options = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Add(
            "x-timestamp",
            DateTimeOffset.UtcNow.ToString("O"));

        if (!string.IsNullOrWhiteSpace(_options.FunctionKey))
        {
            request.Headers.Add(
                "x-functions-key",
                _options.FunctionKey);
        }

        var token = await _tokenStorage.GetTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(
            request,
            cancellationToken);
    }
}