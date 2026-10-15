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
        // response helpers use synchronous HttpResponseData.WriteString which Kestrel blocks by default
        services.Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true);

        var appConfig = new AppConfiguration();
        context.Configuration.Bind(appConfig);
        services.AddSingleton(appConfig);

        var isLocal = appConfig.Environment == EnvironmentType.Local;
        if (!string.Equals(appConfig.DatabaseProvider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This application requires DatabaseProvider=PostgreSql.");
        if (!isLocal)
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
        }

        if (string.IsNullOrWhiteSpace(appConfig.PostgreSqlConnectionString))
            throw new InvalidOperationException("PostgreSqlConnectionString must be configured.");

        var postgresConfig = new Npgsql.NpgsqlConnectionStringBuilder(appConfig.PostgreSqlConnectionString);
        if (isLocal && !IsLoopbackPostgresHost(postgresConfig.Host))
            throw new InvalidOperationException("Local development only allows PostgreSQL on localhost or a loopback IP address.");

        if (isLocal && !string.Equals(context.Configuration["AzureWebJobsStorage"], "UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Local development requires AzureWebJobsStorage=UseDevelopmentStorage=true (Azurite).");

        var dataSource = Npgsql.NpgsqlDataSource.Create(postgresConfig.ConnectionString);
        services.AddSingleton(dataSource);
        services.AddSingleton<IUsersRepository, PostgresUsersRepository>();
        services.AddSingleton<IFoundationRepository, PostgresFoundationRepository>();
        services.AddSingleton<IFoundationAccessRepository, PostgresFoundationAccessRepository>();
        services.AddSingleton<IDashboardConfigurationRepository, PostgresDashboardConfigurationRepository>();
        services.AddSingleton<IGitHubSourceDataRepository, PostgresGitHubSourceDataRepository>();

        if (isLocal && !string.IsNullOrWhiteSpace(appConfig.GitHubAccessToken))
            throw new InvalidOperationException("GitHubAccessToken must not be configured in Local mode; local development never connects to GitHub.");
        if (!isLocal && !string.IsNullOrWhiteSpace(appConfig.GitHubAccessToken))
        {
            services.AddHttpClient<IGitHubSnapshotSource, GitHubRestSnapshotSource>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appConfig.GitHubAccessToken.Trim());
            });
        }

        // Authentication is required in every environment; Local uses a dedicated
        // password-protected token endpoint and never calls an external identity provider.
        if (!string.IsNullOrWhiteSpace(appConfig.TokenKey))
        {
            var tokenKey = Convert.FromBase64String(appConfig.TokenKey);
            if (tokenKey.Length != 32)
                throw new InvalidOperationException("TokenKey must decode to exactly 32 bytes.");

            services.AddSingleton<ISecurityManager>(sp => new BasicSecurityManager(
                sp.GetRequiredService<IUsersRepository>(), tokenKey)
            {
                DisableClientCheck = !appConfig.CheckClient
            });
        }
        else
        {
            throw new InvalidOperationException("TokenKey must be configured; anonymous authentication is disabled.");
        }

        if (isLocal && (appConfig.LocalAdminPassword?.Length ?? 0) < 20)
            throw new InvalidOperationException("LocalAdminPassword must contain at least 20 characters in ignored local.settings.json.");
        if (isLocal && string.IsNullOrWhiteSpace(appConfig.LocalAdminEmail))
            throw new InvalidOperationException("LocalAdminEmail must be set in ignored local.settings.json for local-only admin access.");

        services.AddTransient<ICommonGateway, CommonGateway>();
    })
    .Build();

host.Run();

static bool IsLoopbackPostgresHost(string? host)
{
    if (string.IsNullOrWhiteSpace(host))
        return false;

    return host.Split(',', StringSplitOptions.TrimEntries).All(candidate =>
        string.Equals(candidate, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(candidate, "127.0.0.1", StringComparison.Ordinal)
        || string.Equals(candidate, "::1", StringComparison.Ordinal));
}

