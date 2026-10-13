using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DesignAutomation.AppCatalog.Config;
using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Interface;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // response helpers use synchronous HttpResponseData.WriteString which Kestrel blocks by default
        services.Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true);

        var appConfig = new AppConfiguration();
        context.Configuration.Bind(appConfig);
        services.AddSingleton(appConfig);

        var githubConfig = new GithubAPIConfiguration
        {
            GitHubToken = appConfig.GitHubToken,
            Topic = appConfig.Topic,
            Environment = appConfig.Environment,
        };
        services.AddSingleton(githubConfig);

        services.AddSingleton<IAppHeaderRepository>(_ =>
            new AppHeaderRepository(appConfig.PrimaryDBConnStr, appConfig.PrimaryDatabase, "Apps"));
        services.AddSingleton<IUsersRepository>(_ =>
            new UsersMongoRepository(appConfig.PrimaryDBConnStr, appConfig.PrimaryDatabase, "Users"));

        services.AddSingleton<IGitHubRepository>(sp =>
        {
            if (!githubConfig.IsValid())
                return new UnconfiguredGitHubRepository();
            return new GitHubRepository(githubConfig, sp.GetRequiredService<ILogger<GitHubRepository>>());
        });

        // anonymous security lets all requests through; swap for PingManager in production
        services.AddSingleton<ISecurityManager, AnonynousSecurityManager>();

        services.AddTransient<ICommonGateway, CommonGateway>();
        services.AddTransient<ISecurityGateway, SecurityGateway>();
    })
    .Build();

host.Run();

// Placeholder used when GitHubToken / Topic are absent from local.settings.json
sealed class UnconfiguredGitHubRepository : IGitHubRepository
{
    private static Task<T> Fail<T>() => Task.FromException<T>(
        new InvalidOperationException("GitHub is not configured. Add GitHubToken and Topic to local.settings.json."));

    public Task<List<ReadmeDTO>> ListReposByTopic(string topic) => Fail<List<ReadmeDTO>>();
    public Task<List<ReadmeData>> ParseReposByTopic(string topic) => Fail<List<ReadmeData>>();
    public Task<ReadmeData> ParseRepo(long id) => Fail<ReadmeData>();
}
