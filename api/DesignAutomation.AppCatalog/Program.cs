using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DesignAutomation.AppCatalog.Config;
using DesignAutomation.AppCatalog.Security;
using DesignAutomation.AppCatalog.Interface;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Model;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.GetEnumValue<EnvironmentType>(EnvironmentType.Local) ?? EnvironmentType.Local;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureLogging((ctx, log) =>
    {
        log.ClearProviders();

        string? logLevel = ctx.Configuration["minLogLevel"];
        string? sentry = ctx.Configuration["SentryDSN"];
        
        LogLevel minLevel = !String.IsNullOrWhiteSpace(logLevel) ? logLevel.GetEnumValue<LogLevel>(LogLevel.Trace, true) : LogLevel.Trace;
        log.SetMinimumLevel(minLevel);

        // add sentry
        log.AddSentry( options => {
            options.Dsn = sentry;
            options.EnableLogs = true;
            options.MinimumEventLevel = minLevel;
            options.AttachStacktrace = true;
            options.Environment = $"{env}";
        });
        
        //log.AddProvider(NullLoggerProvider.Instance);
    })
    .ConfigureServices((ctx, services) =>
    {
        // load config values
        AppConfiguration config = ctx.Configuration.Get<AppConfiguration>() ?? new();
        config.Environment = env;
        GithubAPIConfiguration gitConfig = ctx.Configuration.Get<GithubAPIConfiguration>() ?? new();
        gitConfig.Environment = env;

        //services.AddApplicationInsightsTelemetryWorkerService();
        //services.ConfigureFunctionsApplicationInsights();

        // add AppConfig to services -> technically we should try to avoid using AppConfig downstream
        services.AddSingleton<AppConfiguration>( config );
        services.AddSingleton<GithubAPIConfiguration>( gitConfig );

        // inject different security managers for different environments
        //if (config.Environment > EnvironmentType.Local)
        services.AddScoped<ISecurityManager, BasicSecurityManager>( s => new BasicSecurityManager( new UsersMongoRepository( config.PrimaryDBConnStr, config.PrimaryDatabase, "Users" ), config.SigningKey ) { DisableClientCheck = !config.CheckClient} );
        // else // no security on the back end
        // {
        //     services.AddScoped<ISecurityManager, AnonynousSecurityManager>();
        // }

        // load repos
        services.AddScoped<IGitHubRepository, GitHubRepository>();
        services.AddScoped<IAppHeaderRepository>(c => new AppHeaderRepository(config.PrimaryDBConnStr, config.PrimaryDatabase, "Apps" ));
        services.AddScoped<IUsersRepository>(c => new UsersMongoRepository( config.PrimaryDBConnStr, config.PrimaryDatabase, "Users" ));

        // create gateways
        services.AddScoped<ISecurityGateway, SecurityGateway>();
        services.AddScoped<ICommonGateway, CommonGateway>();
    })
    .Build();

host.Run();