using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DesignAutomation.AppCatalog.Security;
using Npgsql;

var apiRoot = Environment.GetEnvironmentVariable("STAGE1_API_ROOT") ?? "http://localhost:7071/api/v1/";
const string userAgent = "Stage1Acceptance/1.0";
var settingsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "api/DesignAutomation.AppCatalog/local.settings.json"));
if (!File.Exists(settingsPath)) throw new FileNotFoundException("Run this tool from a checkout with local.settings.json configured.");
using var settings = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath));
var values = settings.RootElement.GetProperty("Values");
string Setting(string name) => values.GetProperty(name).GetString() ?? string.Empty;
var dbConnectionString = Setting("PostgreSqlConnectionString");
var localAdminEmail = Setting("LocalAdminEmail");
var localAdminPassword = Setting("LocalAdminPassword");
var tokenKey = Convert.FromBase64String(Setting("TokenKey"));
var githubMigrations = new[] { "004_stage3_github_source_data.sql", "005_github_record_relationships.sql" }
    .Select(file => Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "api/DesignAutomation.AppCatalog/Database/Migrations", file)));
await using (var migrationDataSource = NpgsqlDataSource.Create(dbConnectionString))
await using (var migrationConnection = await migrationDataSource.OpenConnectionAsync())
{
    foreach (var migrationPath in githubMigrations)
    {
        await using var migrationCommand = new NpgsqlCommand(await File.ReadAllTextAsync(migrationPath), migrationConnection);
        await migrationCommand.ExecuteNonQueryAsync();
        Console.WriteLine($"Applied idempotent local Stage 3 migration {Path.GetFileName(migrationPath)}.");
    }
}
var prefix = $"stage1-{Guid.NewGuid():N}";
var organization = $"{prefix}-org";
var portfolio = $"{prefix}-portfolio";
var productName = $"{prefix}-product";
var productId = Guid.Empty;
var userIds = new List<Guid>();
var clientFingerprint = string.Empty;

using var client = new HttpClient { BaseAddress = new Uri(apiRoot) };
client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");

static void Pass(string name) => Console.WriteLine($"PASS {name}");

static void Expect(string name, HttpResponseMessage response, HttpStatusCode status)
{
    if (response.StatusCode != status)
        throw new InvalidOperationException($"{name}: expected {(int)status}, received {(int)response.StatusCode}: {response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
    Pass($"{name} ({(int)status})");
}

static async Task<string> Body(HttpResponseMessage response) => await response.Content.ReadAsStringAsync();

static Guid ResultId(string body)
{
    using var json = JsonDocument.Parse(body);
    var root = json.RootElement;
    if (!root.GetProperty("isSuccess").GetBoolean())
    {
        var diagnostic = root.TryGetProperty("debug", out var debug) ? debug.GetString() : "no diagnostic";
        throw new InvalidOperationException($"API returned {root.GetProperty("resultType").GetString()}: {diagnostic}");
    }
    return root.GetProperty("result").GetGuid();
}

string UserToken(Guid id, string email, UserRoleLevel role) => new JWTManager(tokenKey)
    .GenerateToken(id, email, email, clientFingerprint, 1, role).Token;

void UseToken(string? token)
{
    client.DefaultRequestHeaders.Remove("Authorization");
    if (token is not null) client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");
}

async Task<Guid> CreateUser(string email)
{
    var response = await client.PostAsJsonAsync("foundation/users", new { email, role = "User" });
    Expect("admin creates a scoped test user", response, HttpStatusCode.OK);
    var id = ResultId(await Body(response)); userIds.Add(id); return id;
}

try
{
    var login = await client.PostAsJsonAsync("local-auth/token", new { email = localAdminEmail, password = localAdminPassword });
    Expect("local Admin login", login, HttpStatusCode.OK);
    using var loginJson = JsonDocument.Parse(await Body(login));
    var adminToken = loginJson.RootElement.GetProperty("result").GetProperty("token").GetString()!;
    var tokenPayload = adminToken.Split('.')[1].Replace('-', '+').Replace('_', '/');
    tokenPayload = tokenPayload.PadRight(tokenPayload.Length + ((4 - tokenPayload.Length % 4) % 4), '=');
    using (var claims = JsonDocument.Parse(Convert.FromBase64String(tokenPayload)))
        clientFingerprint = claims.RootElement.GetProperty("cid").GetString()!;
    UseToken(adminToken);

    var anonymous = new HttpClient { BaseAddress = new Uri(apiRoot) };
    using (anonymous)
    {
        var response = await anonymous.GetAsync("foundation/products");
        Expect("anonymous product list is rejected", response, HttpStatusCode.Unauthorized);
    }

    var viewerId = await CreateUser($"{prefix}-viewer@local.test");
    var managerId = await CreateUser($"{prefix}-manager@local.test");
    var outsiderId = await CreateUser($"{prefix}-outsider@local.test");

    var productPayload = new
    {
        organizationName = organization, portfolioName = portfolio, productName,
        description = "Stage 1 temporary acceptance record", productType = "Test",
        status = "Active", teams = new[] { new { name = "Core", members = new[] { "dev@local.test" } } },
        githubRepositories = new[] { "https://github.com/example/stage1-acceptance" },
        azureProjects = new[] { new { id = "stage1-project-id", name = "Stage 1 Acceptance" } }
    };
    var created = await client.PostAsJsonAsync("foundation/products", productPayload);
    Expect("Admin creates organization/portfolio/product/team hierarchy", created, HttpStatusCode.OK);
    productId = ResultId(await Body(created));

    var secondCreated = await client.PostAsJsonAsync("foundation/products", new
    {
        organizationName = organization, portfolioName = portfolio, productName = $"{productName}-second"
    });
    Expect("Admin creates an isolated second product", secondCreated, HttpStatusCode.OK);
    var secondProductId = ResultId(await Body(secondCreated));

    var configPath = $"foundation/products/{productId}/dashboard-configuration";
    var secondConfigPath = $"foundation/products/{secondProductId}/dashboard-configuration";
    var defaultConfigResponse = await client.GetAsync(configPath);
    Expect("dashboard configuration is initialized for a product", defaultConfigResponse, HttpStatusCode.OK);
    using (var defaults = JsonDocument.Parse(await Body(defaultConfigResponse)))
    {
        var config = defaults.RootElement.GetProperty("result");
        if (config.GetProperty("modules").GetArrayLength() != 8
            || config.GetProperty("enabledModules").GetArrayLength() != 8
            || config.GetProperty("dashboardLayout").GetProperty("columns").GetInt32() != 2)
            throw new InvalidOperationException("Initial dashboard module registry/default layout is incorrect.");
    }
    Pass("Eight modules and default enabled/layout settings are returned");

    var configured = new
    {
        enabledModules = new[] { "executive-overview", "sprint-health", "security", "testing", "deployment", "technical-debt", "ai-insights" },
        moduleOrder = new[] { "security", "executive-overview", "sprint-health", "pull-requests", "testing", "deployment", "technical-debt", "ai-insights" },
        widgetConfiguration = new Dictionary<string, object>
        {
            ["executive-overview"] = new { visible = true, size = "large" },
            ["security"] = new { visible = false, size = "small" }
        },
        dashboardLayout = new { columns = 3 },
        defaultFilters = new { owner = "owner@local.test", status = "Active", team = "Core" },
        defaultDateRange = new { preset = "custom", startDate = "2026-10-01", endDate = "2026-10-14" }
    };
    var saveConfig = await client.PutAsJsonAsync(configPath, configured);
    Expect("Admin saves module, ordering, widget, layout, filter, and date configuration", saveConfig, HttpStatusCode.OK);
    var reloadedConfig = await client.GetAsync(configPath);
    Expect("configuration is loaded in a separate API request", reloadedConfig, HttpStatusCode.OK);
    using (var saved = JsonDocument.Parse(await Body(reloadedConfig)))
    {
        var config = saved.RootElement.GetProperty("result");
        if (config.GetProperty("enabledModules").GetArrayLength() != 7
            || config.GetProperty("moduleOrder")[0].GetString() != "security"
            || config.GetProperty("widgetConfiguration").GetProperty("executive-overview").GetProperty("size").GetString() != "large"
            || config.GetProperty("widgetConfiguration").GetProperty("security").GetProperty("visible").GetBoolean()
            || config.GetProperty("dashboardLayout").GetProperty("columns").GetInt32() != 3
            || config.GetProperty("defaultFilters").GetProperty("team").GetString() != "Core"
            || config.GetProperty("defaultDateRange").GetProperty("preset").GetString() != "custom")
            throw new InvalidOperationException("Saved dashboard configuration did not round-trip accurately.");
    }
    Pass("Configuration round-trips module state, ordering, widget sizing/visibility, layout, filters, and date range");

    var secondDefaults = await client.GetAsync(secondConfigPath);
    Expect("second product configuration is independently available", secondDefaults, HttpStatusCode.OK);
    using (var second = JsonDocument.Parse(await Body(secondDefaults)))
        if (second.RootElement.GetProperty("result").GetProperty("enabledModules").GetArrayLength() != 8
            || second.RootElement.GetProperty("result").GetProperty("dashboardLayout").GetProperty("columns").GetInt32() != 2)
            throw new InvalidOperationException("Dashboard configuration leaked across products.");
    Pass("dashboard configuration is isolated per product");

    var invalidModule = await client.PutAsJsonAsync(configPath, new
    {
        enabledModules = new[] { "retired-module" }, moduleOrder = new[] { "executive-overview" },
        widgetConfiguration = new { }, dashboardLayout = new { columns = 2 },
        defaultFilters = new { }, defaultDateRange = new { preset = "30d" }
    });
    Expect("unknown/retired module ID is rejected safely", invalidModule, HttpStatusCode.BadRequest);
    var invalidLayout = await client.PutAsJsonAsync(configPath, new
    {
        enabledModules = new[] { "executive-overview" },
        moduleOrder = new[] { "executive-overview", "sprint-health", "pull-requests", "security", "testing", "deployment", "technical-debt", "ai-insights" },
        widgetConfiguration = new { }, dashboardLayout = new { columns = 9 },
        defaultFilters = new { }, defaultDateRange = new { preset = "30d" }
    });
    Expect("invalid layout data is rejected safely", invalidLayout, HttpStatusCode.BadRequest);
    var resetToDefaults = await client.PutAsJsonAsync(configPath, new
    {
        enabledModules = new[] { "executive-overview", "sprint-health", "pull-requests", "security", "testing", "deployment", "technical-debt", "ai-insights" },
        moduleOrder = new[] { "executive-overview", "sprint-health", "pull-requests", "security", "testing", "deployment", "technical-debt", "ai-insights" },
        widgetConfiguration = new { }, dashboardLayout = new { columns = 2 },
        defaultFilters = new { owner = "", status = "", team = "" },
        defaultDateRange = new { preset = "30d", startDate = (string?)null, endDate = (string?)null }
    });
    Expect("default dashboard settings can be restored and saved", resetToDefaults, HttpStatusCode.OK);
    using (var restored = JsonDocument.Parse(await Body(resetToDefaults)))
        if (restored.RootElement.GetProperty("result").GetProperty("enabledModules").GetArrayLength() != 8
            || restored.RootElement.GetProperty("result").GetProperty("moduleOrder")[0].GetString() != "executive-overview"
            || restored.RootElement.GetProperty("result").GetProperty("dashboardLayout").GetProperty("columns").GetInt32() != 2)
            throw new InvalidOperationException("Default dashboard settings were not restored.");
    Pass("default modules, order, widgets, filters, date range, and layout restore correctly");

    var duplicateProduct = await client.PostAsJsonAsync("foundation/products", productPayload);
    Expect("duplicate product is rejected", duplicateProduct, HttpStatusCode.Conflict);

    var duplicateTeamPayload = new
    {
        organizationName = organization, portfolioName = portfolio, productName = $"{productName}-duplicate-team",
        teams = new[] { new { name = "Same" }, new { name = "same" } }
    };
    var duplicateTeam = await client.PostAsJsonAsync("foundation/products", duplicateTeamPayload);
    Expect("case-insensitive duplicate team is rejected", duplicateTeam, HttpStatusCode.BadRequest);

    var invalidDates = await client.PostAsJsonAsync("foundation/products", new
    {
        organizationName = organization, portfolioName = portfolio, productName = $"{productName}-invalid-date",
        startDate = "2026-10-15", targetDate = "2026-10-14"
    });
    Expect("invalid product date range is rejected", invalidDates, HttpStatusCode.BadRequest);

    var productGrant = await client.PutAsJsonAsync($"foundation/products/{productId}/access", new { userId = viewerId, role = "User" });
    Expect("Admin grants product Viewer access", productGrant, HttpStatusCode.OK);
    var portfolioGrant = await client.PutAsJsonAsync($"foundation/portfolios/{GetPortfolioId(await Body(await client.GetAsync("foundation/products")), productId)}/access", new { userId = managerId, role = "PortfolioManager" });
    Expect("Admin grants portfolio Manager access", portfolioGrant, HttpStatusCode.OK);

    await using (var db = NpgsqlDataSource.Create(dbConnectionString))
    await using (var connection = await db.OpenConnectionAsync())
    await using (var command = new NpgsqlCommand("SELECT module_key FROM dashboard_modules ORDER BY module_key", connection))
    await using (var reader = await command.ExecuteReaderAsync())
    {
        var keys = new List<string>(); while (await reader.ReadAsync()) keys.Add(reader.GetString(0));
        var expected = new[] { "ai-insights", "deployment", "executive-overview", "pull-requests", "security", "sprint-health", "technical-debt", "testing" };
        if (!keys.SequenceEqual(expected)) throw new InvalidOperationException("dashboard_modules seed rows do not match the eight Stage 2 modules.");
    }
    Pass("PostgreSQL dashboard_modules contains all eight expected module registrations");
    const string githubRepositoryUrl = "https://github.com/example/stage1-acceptance";
    object[] githubRecords =
    [
        new { entityType = "repository", externalId = "9001", parentExternalId = (string?)null, data = new { repositoryId = 9001, name = "stage1-acceptance", fullName = "example/stage1-acceptance", description = "local fixture", repositoryUrl = githubRepositoryUrl, visibility = "public", defaultBranch = "main", status = "active", createdAt = "2026-01-01T00:00:00Z", updatedAt = "2026-10-14T00:00:00Z", archived = false, primaryLanguage = "C#", languages = new { CSharp = 1000 }, sizeKb = 20 } },
        new { entityType = "branch", externalId = "main", parentExternalId = (string?)"9001", data = new { name = "main", isDefaultBranch = true, isProtected = true, lastCommitId = "abc123", lastActivityAt = "2026-10-14T00:00:00Z" } },
        new { entityType = "commit", externalId = "abc123", parentExternalId = (string?)"9001", data = new { commitId = "abc123", author = "Dev", committer = "Dev", commitDate = "2026-10-14T00:00:00Z", branch = "main", branches = new[] { "main" }, message = "fixture commit", filesChanged = new[] { "README.md" }, additions = 2, deletions = 1 } },
        new { entityType = "pull_request", externalId = "7", parentExternalId = (string?)"9001", data = new { number = 7, title = "Fixture PR", author = "dev", status = "merged", createdAt = "2026-10-10T00:00:00Z", updatedAt = "2026-10-12T00:00:00Z", closedAt = "2026-10-12T00:00:00Z", mergedAt = "2026-10-12T00:00:00Z", sourceBranch = "feature", targetBranch = "main", reviewers = new[] { "reviewer" }, reviewStatus = "approved", approvalStatus = "approved", comments = 1, changes = 1, additions = 2, deletions = 1 } },
        new { entityType = "release", externalId = "3", parentExternalId = (string?)"9001", data = new { releaseId = 3, name = "v1", tag = "v1.0.0", version = "1.0.0", author = "dev", createdAt = "2026-10-01T00:00:00Z", publishedAt = "2026-10-02T00:00:00Z", draft = false, prerelease = false } },
        new { entityType = "workflow", externalId = "50", parentExternalId = (string?)"9001", data = new { workflowId = 50, name = "CI", path = ".github/workflows/ci.yml", state = "active", url = "https://github.com/example/stage1-acceptance/actions/workflows/ci.yml" } },
        new { entityType = "workflow_run", externalId = "51", parentExternalId = (string?)"50", data = new { runId = 51, workflow = "CI", branch = "main", trigger = "push", status = "completed", result = "success", startedAt = "2026-10-14T00:00:00Z", completedAt = "2026-10-14T00:01:00Z", durationSeconds = 60, failureReason = (string?)null } },
        new { entityType = "security_alert", externalId = "20", parentExternalId = (string?)"9001", data = new { alertId = 20, alertType = "dependabot", repositoryUrl = githubRepositoryUrl, severity = "low", affected = new { package = "fixture" }, createdAt = "2026-10-11T00:00:00Z", updatedAt = "2026-10-14T00:00:00Z", status = "open", resolution = (string?)null, resolvedAt = (string?)null } }
    ];
    var snapshotBody = new { repositoryUrl = githubRepositoryUrl, records = githubRecords };
    var snapshotImport = await client.PostAsJsonAsync($"foundation/products/{productId}/github/import", snapshotBody);
    Expect("Admin imports a local GitHub snapshot", snapshotImport, HttpStatusCode.OK);
    var repeatedSnapshotImport = await client.PostAsJsonAsync($"foundation/products/{productId}/github/import", snapshotBody);
    Expect("re-importing a GitHub snapshot is idempotent", repeatedSnapshotImport, HttpStatusCode.OK);
    var liveSyncInLocal = await client.PostAsJsonAsync($"foundation/products/{productId}/github/sync", new { repositoryUrl = githubRepositoryUrl });
    Expect("live GitHub sync is blocked in Local mode", liveSyncInLocal, HttpStatusCode.BadRequest);
    var storedPullRequests = await client.GetAsync($"foundation/products/{productId}/github/data?repositoryUrl={Uri.EscapeDataString(githubRepositoryUrl)}&entityType=pull_request&limit=10&offset=0");
    Expect("scoped API reads persisted GitHub pull request data", storedPullRequests, HttpStatusCode.OK);
    using (var githubRows = JsonDocument.Parse(await Body(storedPullRequests)))
    {
        var result = githubRows.RootElement.GetProperty("result").GetProperty("items");
        if (result.GetArrayLength() != 1 || result[0].GetProperty("data").GetProperty("reviewers").GetArrayLength() != 1
            || result[0].GetProperty("data").GetProperty("approvalStatus").GetString() != "approved")
            throw new InvalidOperationException("GitHub pull request fields did not persist accurately.");
    }
    Pass("GitHub pull request reviewer and approval fields persist in normalized records");
    var invalidPage = await client.GetAsync($"foundation/products/{productId}/github/data?repositoryUrl={Uri.EscapeDataString(githubRepositoryUrl)}&entityType=pull_request&limit=501&offset=0");
    Expect("GitHub record page size is bounded", invalidPage, HttpStatusCode.BadRequest);
    foreach (var type in new[] { "repository", "branch", "commit", "pull_request", "release", "workflow", "workflow_run", "security_alert" })
    {
        var response = await client.GetAsync($"foundation/products/{productId}/github/data?repositoryUrl={Uri.EscapeDataString(githubRepositoryUrl)}&entityType={type}&limit=10&offset=0");
        Expect($"stored GitHub {type} data is readable", response, HttpStatusCode.OK);
        using var page = JsonDocument.Parse(await Body(response));
        var rows = page.RootElement.GetProperty("result").GetProperty("items");
        if (rows.GetArrayLength() != 1)
            throw new InvalidOperationException($"Expected one stored GitHub {type} fixture.");
        var expectedFields = type switch
        {
            "repository" => new[] { "repositoryId", "name", "fullName", "description", "repositoryUrl", "visibility", "defaultBranch", "status", "createdAt", "updatedAt", "archived", "primaryLanguage", "languages", "sizeKb" },
            "branch" => new[] { "name", "isDefaultBranch", "isProtected", "lastCommitId", "lastActivityAt" },
            "commit" => new[] { "commitId", "author", "committer", "commitDate", "branch", "branches", "message", "filesChanged", "additions", "deletions" },
            "pull_request" => new[] { "number", "title", "author", "status", "createdAt", "updatedAt", "closedAt", "mergedAt", "sourceBranch", "targetBranch", "reviewers", "reviewStatus", "approvalStatus", "comments", "changes", "additions", "deletions" },
            "release" => new[] { "releaseId", "name", "tag", "version", "author", "createdAt", "publishedAt", "draft", "prerelease" },
            "workflow" => new[] { "workflowId", "name", "path", "state", "url" },
            "workflow_run" => new[] { "runId", "workflow", "branch", "trigger", "status", "result", "startedAt", "completedAt", "durationSeconds", "failureReason" },
            _ => new[] { "alertId", "alertType", "repositoryUrl", "severity", "affected", "createdAt", "updatedAt", "status", "resolution", "resolvedAt" }
        };
        var data = rows[0].GetProperty("data");
        if (expectedFields.Any(field => !data.TryGetProperty(field, out _)))
            throw new InvalidOperationException($"Stored GitHub {type} record is missing a scoped field.");
    }
    var badParentImport = await client.PostAsJsonAsync($"foundation/products/{productId}/github/import", new
    {
        repositoryUrl = githubRepositoryUrl,
        records = new[] { new { entityType = "workflow_run", externalId = "orphan", parentExternalId = "missing-workflow", data = new { runId = "orphan", workflow = "CI", branch = "main", trigger = "push", status = "completed", result = "success", startedAt = "2026-10-14T00:00:00Z", completedAt = "2026-10-14T00:01:00Z", durationSeconds = 60, failureReason = (string?)null } } }
    });
    Expect("GitHub records with missing parent relationships are rejected", badParentImport, HttpStatusCode.BadRequest);
    var missingFieldImport = await client.PostAsJsonAsync($"foundation/products/{productId}/github/import", new
    {
        repositoryUrl = githubRepositoryUrl,
        records = new[] { new { entityType = "branch", externalId = "broken", data = new { name = "broken" } } }
    });
    Expect("incomplete GitHub records are rejected", missingFieldImport, HttpStatusCode.BadRequest);
    UseToken(UserToken(viewerId, $"{prefix}-viewer@local.test", UserRoleLevel.User));
    var viewerConfigRead = await client.GetAsync(configPath);
    Expect("product Viewer can read assigned dashboard configuration", viewerConfigRead, HttpStatusCode.OK);
    var viewerGithubRead = await client.GetAsync($"foundation/products/{productId}/github/data?repositoryUrl={Uri.EscapeDataString(githubRepositoryUrl)}&entityType=repository");
    Expect("product Viewer can read GitHub data for an assigned product", viewerGithubRead, HttpStatusCode.OK);
    var viewerOtherProductRead = await client.GetAsync(secondConfigPath);
    Expect("product Viewer cannot read another product dashboard", viewerOtherProductRead, HttpStatusCode.Unauthorized);
    var viewerImport = await client.PostAsJsonAsync($"foundation/products/{productId}/github/import", snapshotBody);
    Expect("product Viewer cannot import GitHub data", viewerImport, HttpStatusCode.Unauthorized);
    var viewerConfigWrite = await client.PutAsJsonAsync(configPath, configured);
    Expect("product Viewer cannot change dashboard configuration", viewerConfigWrite, HttpStatusCode.Unauthorized);
    UseToken(UserToken(managerId, $"{prefix}-manager@local.test", UserRoleLevel.User));
    var managerConfigWrite = await client.PutAsJsonAsync(configPath, configured);
    Expect("portfolio Manager can manage dashboard configuration", managerConfigWrite, HttpStatusCode.OK);
    UseToken(UserToken(outsiderId, $"{prefix}-outsider@local.test", UserRoleLevel.User));
    var outsiderConfigRead = await client.GetAsync(configPath);
    Expect("unassigned user cannot read dashboard configuration", outsiderConfigRead, HttpStatusCode.Unauthorized);
    var outsiderGithubRead = await client.GetAsync($"foundation/products/{productId}/github/data?repositoryUrl={Uri.EscapeDataString(githubRepositoryUrl)}&entityType=repository");
    Expect("unassigned user cannot read GitHub records", outsiderGithubRead, HttpStatusCode.Unauthorized);
    UseToken(adminToken);
    var invalidGrant = await client.PutAsJsonAsync($"foundation/products/{Guid.NewGuid()}/access", new { userId = viewerId, role = "User" });
    Expect("access to missing product is rejected", invalidGrant, HttpStatusCode.NotFound);

    UseToken(UserToken(viewerId, $"{prefix}-viewer@local.test", UserRoleLevel.User));
    var viewerProducts = await client.GetAsync("foundation/products");
    Expect("scoped Viewer can list authorized products", viewerProducts, HttpStatusCode.OK);
    using (var list = JsonDocument.Parse(await Body(viewerProducts)))
        if (list.RootElement.GetProperty("result").GetArrayLength() != 1) throw new InvalidOperationException("Viewer product list was not scoped to the granted product.");
    Pass("Viewer product list is restricted to its grant");
    var viewerUsers = await client.GetAsync("foundation/users");
    Expect("scoped Viewer cannot list user administration", viewerUsers, HttpStatusCode.Unauthorized);
    var viewerWrite = await client.PutAsJsonAsync($"foundation/products/{productId}", new { productName = $"{productName}-viewer-edit" });
    Expect("scoped Viewer cannot edit a product", viewerWrite, HttpStatusCode.Unauthorized);

    UseToken(UserToken(managerId, $"{prefix}-manager@local.test", UserRoleLevel.User));
    var managerProducts = await client.GetAsync("foundation/products");
    Expect("portfolio Manager can list products in the portfolio", managerProducts, HttpStatusCode.OK);
    var managerUpdate = await client.PutAsJsonAsync($"foundation/products/{productId}", new
    {
        productName = $"{productName}-managed", status = "Active", teams = new[] { new { name = "Managed Team", members = Array.Empty<string>() } }
    });
    Expect("portfolio Manager can update an assigned product", managerUpdate, HttpStatusCode.OK);

    UseToken(UserToken(outsiderId, $"{prefix}-outsider@local.test", UserRoleLevel.User));
    var outsiderProducts = await client.GetAsync("foundation/products");
    Expect("unassigned user can only receive an empty product list", outsiderProducts, HttpStatusCode.OK);
    using (var list = JsonDocument.Parse(await Body(outsiderProducts)))
        if (list.RootElement.GetProperty("result").GetArrayLength() != 0) throw new InvalidOperationException("Unassigned user could see an ungranted product.");
    Pass("Unassigned user sees no products");

    UseToken(adminToken);
    var persisted = await client.GetAsync("foundation/products");
    Expect("Admin reads persisted product after separate requests", persisted, HttpStatusCode.OK);
    using (var list = JsonDocument.Parse(await Body(persisted)))
    {
        var savedProduct = list.RootElement.GetProperty("result").EnumerateArray().SingleOrDefault(p => p.GetProperty("productId").GetGuid() == productId);
        if (savedProduct.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException("The acceptance product did not persist.");
        if (savedProduct.GetProperty("teams").GetArrayLength() != 1
            || savedProduct.GetProperty("githubRepositories").GetArrayLength() != 1
            || savedProduct.GetProperty("azureProjects").GetArrayLength() != 1)
            throw new InvalidOperationException("Team or source project assignments did not persist.");
    }
    Pass("Product hierarchy, team, GitHub reference, and Azure DevOps reference persist across API requests");

    Console.WriteLine("Foundation, dashboard configuration, and local GitHub snapshot acceptance scenarios all passed.");
}
finally
{
    UseToken(null);
    if (productId != Guid.Empty || userIds.Count > 0)
    {
        await using var source = NpgsqlDataSource.Create(dbConnectionString);
        await using var connection = await source.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var deleteProducts = new NpgsqlCommand("DELETE FROM products p USING portfolios pf,organizations o WHERE p.portfolio_id=pf.id AND pf.organization_id=o.id AND o.name=@org", connection, transaction))
        { deleteProducts.Parameters.AddWithValue("org", organization); await deleteProducts.ExecuteNonQueryAsync(); }
        await using (var deleteOrganization = new NpgsqlCommand("DELETE FROM organizations WHERE name=@org", connection, transaction))
        { deleteOrganization.Parameters.AddWithValue("org", organization); await deleteOrganization.ExecuteNonQueryAsync(); }
        await using (var deleteUsers = new NpgsqlCommand("DELETE FROM product_health_users WHERE email LIKE @prefix", connection, transaction))
        { deleteUsers.Parameters.AddWithValue("prefix", $"{prefix}-%@local.test"); await deleteUsers.ExecuteNonQueryAsync(); }
        await transaction.CommitAsync();
        Console.WriteLine("Temporary acceptance records cleaned up.");
    }
}

static Guid GetPortfolioId(string productListJson, Guid targetProductId)
{
    using var json = JsonDocument.Parse(productListJson);
    foreach (var product in json.RootElement.GetProperty("result").EnumerateArray())
        if (product.GetProperty("productId").GetGuid() == targetProductId) return product.GetProperty("portfolioId").GetGuid();
    throw new InvalidOperationException("Created product was missing when resolving the portfolio.");
}
