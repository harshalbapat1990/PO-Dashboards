using DesignAutomation.AppCatalog.Model;
using Npgsql;
using NpgsqlTypes;

namespace DesignAutomation.AppCatalog.Data;

/// <summary>Initial Stage 0 organization/product setup persistence.</summary>
public sealed class PostgresFoundationRepository(NpgsqlDataSource dataSource) : IFoundationRepository
{
    public async Task<List<FoundationProductSummary>> ListProducts()
    {
        var products = new Dictionary<Guid, FoundationProductSummary>();
        await using var connection = await dataSource.OpenConnectionAsync();
        const string sql = "SELECT o.id,o.name,pf.id,pf.name,p.id,p.name,p.description,p.product_type,p.product_owner,p.product_manager,p.status,p.global_product_lead,p.engineering_manager,p.scrum_master,p.security_lead,p.start_date,p.target_date,COALESCE((SELECT organization FROM integrations i WHERE i.product_id=p.id AND i.provider='AzureDevOps' AND i.organization<>'' ORDER BY i.updated_at DESC LIMIT 1),'') FROM organizations o JOIN portfolios pf ON pf.organization_id=o.id JOIN products p ON p.portfolio_id=pf.id ORDER BY o.name,pf.name,p.name";
        await using (var command = new NpgsqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var summary = new FoundationProductSummary
                {
                    OrganizationId = reader.GetGuid(0), OrganizationName = reader.GetString(1),
                    PortfolioId = reader.GetGuid(2), PortfolioName = reader.GetString(3),
                    ProductId = reader.GetGuid(4), ProductName = reader.GetString(5), Description = reader.GetString(6),
                    ProductType = reader.GetString(7), ProductOwner = reader.GetString(8), ProductManager = reader.GetString(9),
                    Status = reader.GetString(10), GlobalProductLead = reader.GetString(11), EngineeringManager = reader.GetString(12),
                    ScrumMaster = reader.GetString(13), SecurityLead = reader.GetString(14),
                    StartDate = reader.IsDBNull(15) ? null : reader.GetFieldValue<DateOnly>(15),
                    TargetDate = reader.IsDBNull(16) ? null : reader.GetFieldValue<DateOnly>(16),
                    AzureOrganization = reader.GetString(17)
                };
                products.Add(summary.ProductId, summary);
            }
        }

        foreach (var productId in products.Keys.ToArray())
        {
            var repositories = new List<string>();
            await using (var command = new NpgsqlCommand("SELECT repository_url FROM product_github_repositories WHERE product_id=@id ORDER BY repository_url", connection))
            {
                command.Parameters.AddWithValue("id", productId);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) repositories.Add(reader.GetString(0));
            }
            var projects = new List<FoundationAzureProjectRequest>();
            await using (var command = new NpgsqlCommand("SELECT project_id,project_name FROM product_azure_projects WHERE product_id=@id ORDER BY project_name", connection))
            {
                command.Parameters.AddWithValue("id", productId);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) projects.Add(new() { Id = reader.GetString(0), Name = reader.GetString(1) });
            }
            var teams = new List<FoundationTeamRequest>();
            var teamRows = new List<(Guid Id, string Name)>();
            await using (var command = new NpgsqlCommand("SELECT id,name FROM teams WHERE product_id=@id ORDER BY name", connection))
            {
                command.Parameters.AddWithValue("id", productId);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) teamRows.Add((reader.GetGuid(0), reader.GetString(1)));
            }
            foreach (var team in teamRows)
            {
                var members = new List<string>();
                await using var memberCommand = new NpgsqlCommand("SELECT member_email FROM team_members WHERE team_id=@team ORDER BY member_email", connection);
                memberCommand.Parameters.AddWithValue("team", team.Id);
                await using var memberReader = await memberCommand.ExecuteReaderAsync();
                while (await memberReader.ReadAsync()) members.Add(memberReader.GetString(0));
                teams.Add(new FoundationTeamRequest { Name = team.Name, Members = members });
            }
            products[productId] = products[productId] with { GitHubRepositories = repositories, AzureProjects = projects, Teams = teams };
        }
        return products.Values.ToList();
    }

    public async Task<Guid> CreateProduct(FoundationProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var organizationName = Required(request.OrganizationName, "Organization name");
        var portfolioName = Required(request.PortfolioName, "Portfolio name");
        var productName = Required(request.ProductName, "Product name");
        ValidateLength(organizationName, 200, "Organization name");
        ValidateLength(portfolioName, 200, "Portfolio name");
        ValidateLength(productName, 200, "Product name");
        ValidateLength(request.Description, 4000, "Description");
        ValidateLength(request.ProductType, 200, "Product type");
        ValidateLength(request.ProductOwner, 320, "Product owner");
        ValidateLength(request.ProductManager, 320, "Product manager");
        ValidateLength(request.GlobalProductLead, 320, "Global product lead");
        ValidateLength(request.EngineeringManager, 320, "Engineering manager");
        ValidateLength(request.ScrumMaster, 320, "Scrum master");
        ValidateLength(request.SecurityLead, 320, "Security lead");
        if (!new[] { "Active", "Planned", "Paused", "Retired" }.Contains(request.Status, StringComparer.Ordinal))
            throw new ArgumentException("Status must be Active, Planned, Paused, or Retired.");
        if (request.StartDate.HasValue && request.TargetDate.HasValue && request.TargetDate < request.StartDate)
            throw new ArgumentException("Target date cannot be earlier than the start date.");
        ValidateLength(request.GitHubOrganization, 200, "GitHub organization reference");
        ValidateLength(request.AzureOrganization, 200, "Azure DevOps organization reference");
        ValidateLength(request.GitHubSecretReference, 500, "GitHub secret reference");
        ValidateLength(request.AzureSecretReference, 500, "Azure DevOps secret reference");
        if (request.SyncFrequencyMinutes is < 1 or > 10080)
            throw new ArgumentException("Synchronization frequency must be between 1 and 10080 minutes.");
        if ((request.Teams?.Count ?? 0) > 100 || (request.GitHubRepositories?.Count ?? 0) > 200 || (request.AzureProjects?.Count ?? 0) > 200)
            throw new ArgumentException("A product can include at most 100 teams and 200 repositories or Azure DevOps projects.");
        EnsureUniqueTeams(request.Teams);
        foreach (var team in request.Teams ?? [])
        {
            if (team is null) throw new ArgumentException("Team entries cannot be empty.");
            ValidateLength(Required(team.Name, "Team name"), 200, "Team name");
            if ((team.Members?.Count ?? 0) > 500) throw new ArgumentException("A team can include at most 500 members.");
            foreach (var member in team.Members ?? [])
            {
                if (string.IsNullOrWhiteSpace(member)) continue;
                ValidateLength(member, 320, "Team member");
            }
        }
        foreach (var repository in request.GitHubRepositories ?? [])
        {
            if (!Uri.TryCreate(repository, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("GitHub repository references must be HTTPS URLs.");
        }
        foreach (var project in request.AzureProjects ?? [])
        {
            if (project is null) throw new ArgumentException("Azure DevOps project entries cannot be empty.");
            ValidateLength(project.Id, 200, "Azure DevOps project ID");
            ValidateLength(project.Name, 200, "Azure DevOps project name");
        }
        var productId = Guid.NewGuid();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var organizationId = await FindOrCreate(connection, transaction,
            "INSERT INTO organizations(id,name) VALUES(@id,@name) ON CONFLICT (lower(name)) DO UPDATE SET name=EXCLUDED.name RETURNING id",
            Guid.NewGuid(), organizationName);
        var portfolioId = await FindOrCreate(connection, transaction,
            "INSERT INTO portfolios(id,organization_id,name) VALUES(@id,@parent,@name) ON CONFLICT (organization_id,lower(name)) DO UPDATE SET name=EXCLUDED.name RETURNING id",
            Guid.NewGuid(), portfolioName, organizationId);

        await using (var command = new NpgsqlCommand("INSERT INTO products(id,portfolio_id,name,description,product_type,product_owner,product_manager,status,global_product_lead,engineering_manager,scrum_master,security_lead,start_date,target_date) VALUES(@id,@parent,@name,@description,@type,@owner,@manager,@status,@lead,@eng,@scrum,@security,@start,@target)", connection, transaction))
        {
            command.Parameters.AddWithValue("id", productId); command.Parameters.AddWithValue("parent", portfolioId);
            command.Parameters.AddWithValue("name", productName); command.Parameters.AddWithValue("description", (request.Description ?? string.Empty).Trim());
            command.Parameters.AddWithValue("type", (request.ProductType ?? string.Empty).Trim()); command.Parameters.AddWithValue("owner", (request.ProductOwner ?? string.Empty).Trim());
            command.Parameters.AddWithValue("manager", (request.ProductManager ?? string.Empty).Trim());
            command.Parameters.AddWithValue("status", request.Status);
            command.Parameters.AddWithValue("lead", (request.GlobalProductLead ?? string.Empty).Trim());
            command.Parameters.AddWithValue("eng", (request.EngineeringManager ?? string.Empty).Trim());
            command.Parameters.AddWithValue("scrum", (request.ScrumMaster ?? string.Empty).Trim());
            command.Parameters.AddWithValue("security", (request.SecurityLead ?? string.Empty).Trim());
            command.Parameters.AddWithValue("start", (object?)request.StartDate ?? DBNull.Value);
            command.Parameters.AddWithValue("target", (object?)request.TargetDate ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
        }

        foreach (var team in (request.Teams ?? []).Where(t => !string.IsNullOrWhiteSpace(t.Name)))
        {
            var teamId = Guid.NewGuid();
            await using (var command = new NpgsqlCommand("INSERT INTO teams(id,product_id,name) VALUES(@id,@product,@name) ON CONFLICT(product_id,name) DO NOTHING", connection, transaction))
            {
                command.Parameters.AddWithValue("id", teamId); command.Parameters.AddWithValue("product", productId); command.Parameters.AddWithValue("name", team.Name.Trim());
                await command.ExecuteNonQueryAsync();
            }
            await using (var select = new NpgsqlCommand("SELECT id FROM teams WHERE product_id=@product AND name=@name", connection, transaction))
            {
                select.Parameters.AddWithValue("product", productId); select.Parameters.AddWithValue("name", team.Name.Trim());
                teamId = (Guid)(await select.ExecuteScalarAsync() ?? throw new InvalidOperationException("Team insert could not be read back."));
            }
            foreach (var member in (team.Members ?? []).Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await using var command = new NpgsqlCommand("INSERT INTO team_members(team_id,member_email) VALUES(@team,@member) ON CONFLICT DO NOTHING", connection, transaction);
                command.Parameters.AddWithValue("team", teamId); command.Parameters.AddWithValue("member", member);
                await command.ExecuteNonQueryAsync();
            }
        }

        var githubRepositories = (request.GitHubRepositories ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var repo in githubRepositories)
        {
            await using var command = new NpgsqlCommand("INSERT INTO product_github_repositories(product_id,repository_url) VALUES(@product,@url) ON CONFLICT DO NOTHING", connection, transaction);
            command.Parameters.AddWithValue("product", productId); command.Parameters.AddWithValue("url", repo);
            await command.ExecuteNonQueryAsync();
        }

        var azureProjects = (request.AzureProjects ?? []).Where(x => x is not null && !string.IsNullOrWhiteSpace(x.Id)).ToList();
        foreach (var project in azureProjects)
        {
            await using var command = new NpgsqlCommand("INSERT INTO product_azure_projects(product_id,project_id,project_name) VALUES(@product,@id,@name) ON CONFLICT DO NOTHING", connection, transaction);
            command.Parameters.AddWithValue("product", productId); command.Parameters.AddWithValue("id", project.Id.Trim()); command.Parameters.AddWithValue("name", (project.Name ?? string.Empty).Trim());
            await command.ExecuteNonQueryAsync();
        }

        if (githubRepositories.Count == 0 && !string.IsNullOrWhiteSpace(request.GitHubOrganization))
            await InsertIntegration(connection, transaction, productId, "GitHub", request.GitHubOrganization ?? string.Empty, string.Empty, request.GitHubSecretReference ?? string.Empty, request.SyncFrequencyMinutes);
        foreach (var repo in githubRepositories)
            await InsertIntegration(connection, transaction, productId, "GitHub", request.GitHubOrganization ?? string.Empty, repo, request.GitHubSecretReference ?? string.Empty, request.SyncFrequencyMinutes);
        if (azureProjects.Count == 0 && !string.IsNullOrWhiteSpace(request.AzureOrganization))
            await InsertIntegration(connection, transaction, productId, "AzureDevOps", request.AzureOrganization ?? string.Empty, string.Empty, request.AzureSecretReference ?? string.Empty, request.SyncFrequencyMinutes);
        foreach (var project in azureProjects)
            await InsertIntegration(connection, transaction, productId, "AzureDevOps", request.AzureOrganization ?? string.Empty, project.Id.Trim(), request.AzureSecretReference ?? string.Empty, request.SyncFrequencyMinutes);

        await using (var config = new NpgsqlCommand("INSERT INTO product_dashboard_configuration(product_id) VALUES(@product)", connection, transaction))
        {
            config.Parameters.AddWithValue("product", productId);
            await config.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        return productId;
    }

    public async Task<bool> UpdateProduct(Guid productId, FoundationProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = Required(request.ProductName, "Product name");
        ValidateLength(name, 200, "Product name"); ValidateLength(request.Description, 4000, "Description");
        ValidateLength(request.ProductType, 200, "Product type");
        foreach (var value in new[] { request.ProductOwner, request.ProductManager, request.GlobalProductLead, request.EngineeringManager, request.ScrumMaster, request.SecurityLead }) ValidateLength(value, 320, "Product role contact");
        if (!new[] { "Active", "Planned", "Paused", "Retired" }.Contains(request.Status, StringComparer.Ordinal)) throw new ArgumentException("Status must be Active, Planned, Paused, or Retired.");
        if (request.StartDate.HasValue && request.TargetDate.HasValue && request.TargetDate < request.StartDate) throw new ArgumentException("Target date cannot be earlier than the start date.");
        if ((request.Teams?.Count ?? 0) > 100 || (request.Teams ?? []).Any(t => t is null || string.IsNullOrWhiteSpace(t.Name) || t.Name.Trim().Length > 200 || (t.Members?.Count ?? 0) > 500))
            throw new ArgumentException("Provide up to 100 named teams, with no more than 500 members per team.");
        if (request.SyncFrequencyMinutes is < 1 or > 10080)
            throw new ArgumentException("Sync frequency must be between 1 and 10080 minutes.");
        EnsureUniqueTeams(request.Teams);
        var azureProjects = (request.AzureProjects ?? []).Where(project => project is not null).ToList();
        if (azureProjects.Count > 100 || azureProjects.Any(project => string.IsNullOrWhiteSpace(project.Id)
            || string.IsNullOrWhiteSpace(project.Name) || project.Id.Trim().Length > 200 || project.Name.Trim().Length > 200))
            throw new ArgumentException("Provide up to 100 Azure DevOps projects with IDs and names no longer than 200 characters.");
        if (azureProjects.Select(project => project.Id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != azureProjects.Count)
            throw new ArgumentException("Azure DevOps project IDs must be unique within a product.");
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var command = new NpgsqlCommand("UPDATE products SET name=@name,description=@description,product_type=@type,product_owner=@owner,product_manager=@manager,status=@status,global_product_lead=@lead,engineering_manager=@eng,scrum_master=@scrum,security_lead=@security,start_date=@start,target_date=@target,updated_at=now() WHERE id=@id", connection, transaction))
        {
            command.Parameters.AddWithValue("id", productId); command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("description", (request.Description ?? string.Empty).Trim()); command.Parameters.AddWithValue("type", (request.ProductType ?? string.Empty).Trim());
            command.Parameters.AddWithValue("owner", (request.ProductOwner ?? string.Empty).Trim()); command.Parameters.AddWithValue("manager", (request.ProductManager ?? string.Empty).Trim());
            command.Parameters.AddWithValue("status", request.Status); command.Parameters.AddWithValue("lead", (request.GlobalProductLead ?? string.Empty).Trim());
            command.Parameters.AddWithValue("eng", (request.EngineeringManager ?? string.Empty).Trim()); command.Parameters.AddWithValue("scrum", (request.ScrumMaster ?? string.Empty).Trim());
            command.Parameters.AddWithValue("security", (request.SecurityLead ?? string.Empty).Trim()); command.Parameters.AddWithValue("start", (object?)request.StartDate ?? DBNull.Value);
            command.Parameters.AddWithValue("target", (object?)request.TargetDate ?? DBNull.Value);
            if (await command.ExecuteNonQueryAsync() == 0) return false;
        }
        await using (var delete = new NpgsqlCommand("DELETE FROM teams WHERE product_id=@id", connection, transaction)) { delete.Parameters.AddWithValue("id", productId); await delete.ExecuteNonQueryAsync(); }
        foreach (var team in request.Teams ?? [])
        {
            var teamId = Guid.NewGuid();
            await using (var insert = new NpgsqlCommand("INSERT INTO teams(id,product_id,name) VALUES(@id,@product,@name) RETURNING id", connection, transaction))
            { insert.Parameters.AddWithValue("id", teamId); insert.Parameters.AddWithValue("product", productId); insert.Parameters.AddWithValue("name", team.Name.Trim()); teamId = (Guid)(await insert.ExecuteScalarAsync() ?? teamId); }
            foreach (var member in (team.Members ?? []).Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            { ValidateLength(member, 320, "Team member"); await using var add = new NpgsqlCommand("INSERT INTO team_members(team_id,member_email) VALUES(@team,@member)", connection, transaction); add.Parameters.AddWithValue("team", teamId); add.Parameters.AddWithValue("member", member); await add.ExecuteNonQueryAsync(); }
        }
        var projectIds = azureProjects.Select(project => project.Id.Trim()).ToArray();
        foreach (var project in azureProjects)
        {
            await using var upsertProject = new NpgsqlCommand("INSERT INTO product_azure_projects(product_id,project_id,project_name) VALUES(@product,@id,@name) ON CONFLICT(product_id,project_id) DO UPDATE SET project_name=EXCLUDED.project_name", connection, transaction);
            upsertProject.Parameters.AddWithValue("product", productId);
            upsertProject.Parameters.AddWithValue("id", project.Id.Trim());
            upsertProject.Parameters.AddWithValue("name", project.Name.Trim());
            await upsertProject.ExecuteNonQueryAsync();
        }
        await using (var removeProjects = new NpgsqlCommand("DELETE FROM product_azure_projects WHERE product_id=@product AND NOT (project_id = ANY(@projectIds))", connection, transaction))
        {
            removeProjects.Parameters.AddWithValue("product", productId);
            removeProjects.Parameters.AddWithValue("projectIds", NpgsqlDbType.Array | NpgsqlDbType.Text, projectIds);
            await removeProjects.ExecuteNonQueryAsync();
        }
        await using (var removeIntegrations = new NpgsqlCommand("DELETE FROM integrations WHERE product_id=@product AND provider='AzureDevOps' AND NOT (repository_or_project = ANY(@projectIds))", connection, transaction))
        {
            removeIntegrations.Parameters.AddWithValue("product", productId);
            removeIntegrations.Parameters.AddWithValue("projectIds", NpgsqlDbType.Array | NpgsqlDbType.Text, projectIds);
            await removeIntegrations.ExecuteNonQueryAsync();
        }
        foreach (var project in azureProjects)
            await UpsertIntegration(connection, transaction, productId, "AzureDevOps", request.AzureOrganization ?? string.Empty,
                project.Id.Trim(), request.AzureSecretReference ?? string.Empty, request.SyncFrequencyMinutes);
        await transaction.CommitAsync(); return true;
    }

    private static async Task<Guid> FindOrCreate(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, Guid id, string name, Guid parent = default)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", id); command.Parameters.AddWithValue("name", name);
        if (parent != Guid.Empty) command.Parameters.AddWithValue("parent", parent);
        return (Guid)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Foundation record insert returned no ID."));
    }

    private static string Required(string value, string label) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException($"{label} is required.") : value.Trim();

    private static void EnsureUniqueTeams(IEnumerable<FoundationTeamRequest>? teams)
    {
        var names = (teams ?? []).Where(t => t is not null).Select(t => t.Name.Trim());
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count())
            throw new ArgumentException("Team names must be unique within a product.");
    }

    private static void ValidateLength(string? value, int maximum, string label)
    {
        if ((value?.Trim().Length ?? 0) > maximum)
            throw new ArgumentException($"{label} cannot exceed {maximum} characters.");
    }

    private static async Task InsertIntegration(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid productId,
        string provider, string organization, string source, string secretReference, int frequency)
    {
        await using var command = new NpgsqlCommand("INSERT INTO integrations(id,product_id,provider,organization,repository_or_project,secret_reference,sync_frequency_minutes) VALUES(@id,@product,@provider,@organization,@source,@secret,@frequency)", connection, transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid()); command.Parameters.AddWithValue("product", productId);
        command.Parameters.AddWithValue("provider", provider); command.Parameters.AddWithValue("organization", organization.Trim());
        command.Parameters.AddWithValue("source", source); command.Parameters.AddWithValue("secret", secretReference.Trim());
        command.Parameters.AddWithValue("frequency", frequency);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task UpsertIntegration(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid productId,
        string provider, string organization, string source, string secretReference, int frequency)
    {
        await using var update = new NpgsqlCommand("UPDATE integrations SET secret_reference=@secret,sync_frequency_minutes=@frequency,updated_at=now() WHERE product_id=@product AND provider=@provider AND organization=@organization AND repository_or_project=@source", connection, transaction);
        update.Parameters.AddWithValue("product", productId); update.Parameters.AddWithValue("provider", provider);
        update.Parameters.AddWithValue("organization", organization.Trim()); update.Parameters.AddWithValue("source", source);
        update.Parameters.AddWithValue("secret", secretReference.Trim()); update.Parameters.AddWithValue("frequency", frequency);
        if (await update.ExecuteNonQueryAsync() > 0) return;
        await InsertIntegration(connection, transaction, productId, provider, organization, source, secretReference, frequency);
    }
}
