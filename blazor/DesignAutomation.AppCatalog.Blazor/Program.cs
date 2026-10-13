using DesignAutomation.AppCatalog.Blazor.Components;
using DesignAutomation.AppCatalog.Blazor.Models.Configuration;
using DesignAutomation.AppCatalog.Blazor.Services;
using DesignAutomation.AppCatalog.Blazor.Services.Api;
using DesignAutomation.AppCatalog.Blazor.Services.Dashboard;
using DesignAutomation.AppCatalog.Blazor.Services.Search;
using DesignAutomation.AppCatalog.Blazor.Services.Storage;
using DesignAutomation.AppCatalog.Blazor.Services.Authentication;
using DesignAutomation.AppCatalog.Blazor.Features.ProductHealth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));

builder.Services.AddTransient<AppCatalogAuthHandler>();

builder.Services.AddHttpClient<AppCatalogApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<ApiOptions>>();

    client.BaseAddress =
        new Uri(options.Value.BaseUrl);
})
.AddHttpMessageHandler<AppCatalogAuthHandler>();

builder.Services.AddScoped<ITokenStorage, SessionStorageService>();

builder.Services.AddScoped<ProtectedSessionStorage>();

builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider>();

builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<DashboardService>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CacheService>();

builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<ProductHealthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
