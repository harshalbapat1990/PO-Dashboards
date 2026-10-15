using DesignAutomation.AppCatalog.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;

namespace DesignAutomation.AppCatalog.Data;

public sealed class PostgresGitHubSourceDataRepository(NpgsqlDataSource dataSource) : IGitHubSourceDataRepository
{
    private static readonly Dictionary<string, string[]> RequiredFields = new(StringComparer.Ordinal)
    {
        ["repository"] = ["repositoryId", "name", "fullName", "description", "repositoryUrl", "visibility", "defaultBranch", "status", "createdAt", "updatedAt", "archived", "primaryLanguage", "languages", "sizeKb"],
        ["branch"] = ["name", "isDefaultBranch", "isProtected", "lastCommitId", "lastActivityAt"],
        ["commit"] = ["commitId", "author", "committer", "commitDate", "branch", "branches", "message", "filesChanged", "additions", "deletions"],
        ["pull_request"] = ["number", "title", "author", "status", "createdAt", "updatedAt", "closedAt", "mergedAt", "sourceBranch", "targetBranch", "reviewers", "reviewStatus", "approvalStatus", "comments", "changes", "additions", "deletions"],
        ["release"] = ["releaseId", "name", "tag", "version", "author", "createdAt", "publishedAt", "draft", "prerelease"],
        ["workflow"] = ["workflowId", "name", "path", "state", "url"],
        ["workflow_run"] = ["runId", "workflow", "branch", "trigger", "status", "result", "startedAt", "completedAt", "durationSeconds", "failureReason"],
        ["security_alert"] = ["alertId", "alertType", "repositoryUrl", "severity", "affected", "createdAt", "updatedAt", "status", "resolution", "resolvedAt"]
    };
    private static readonly Dictionary<string, string> IdFields = new(StringComparer.Ordinal)
    {
        ["repository"] = "repositoryId", ["branch"] = "name", ["commit"] = "commitId",
        ["pull_request"] = "number", ["release"] = "releaseId", ["workflow"] = "workflowId",
        ["workflow_run"] = "runId", ["security_alert"] = "alertId"
    };

    public async Task<int> Import(Guid productId, string repositoryUrl, IReadOnlyCollection<GitHubSnapshotRecord> records)
    {
        var canonicalUrl = ValidateRepositoryUrl(repositoryUrl);
        if (records is null || records.Count is 0 or > 5000)
            throw new ArgumentException("Provide between 1 and 5000 normalized GitHub records per import.");

        var unique = new HashSet<(string Type, string Id)>();
        foreach (var record in records)
        {
            if (record is null || string.IsNullOrWhiteSpace(record.EntityType)
                || !RequiredFields.TryGetValue(record.EntityType, out var required))
                throw new ArgumentException("A GitHub record has an unsupported entityType.");
            if (string.IsNullOrWhiteSpace(record.ExternalId) || record.ExternalId.Length > 300)
                throw new ArgumentException("Each GitHub record needs an externalId of at most 300 characters.");
            if (!unique.Add((record.EntityType, record.ExternalId)))
                throw new ArgumentException("An import cannot contain duplicate entityType/externalId pairs.");
            if (record.EntityType != "repository" && string.IsNullOrWhiteSpace(record.ParentExternalId))
                throw new ArgumentException($"A {record.EntityType} record must reference its parent externalId.");
            if (record.Data is null || required.Any(field => !record.Data.ContainsKey(field)))
                throw new ArgumentException($"A {record.EntityType} record is missing one or more required normalized fields.");
            if (!string.Equals(record.Data[IdFields[record.EntityType]]?.ToString(), record.ExternalId, StringComparison.Ordinal))
                throw new ArgumentException($"A {record.EntityType} record externalId must match its normalized identifier field.");
            if (record.EntityType == "repository" && !string.Equals(record.Data["repositoryUrl"]?.ToString(), canonicalUrl, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Repository record URL must match the linked repository URL.");
            if (record.EntityType == "security_alert" && !string.Equals(record.Data["repositoryUrl"]?.ToString(), canonicalUrl, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Security alert repositoryUrl must match the linked repository URL.");
            if (record.Data.ToString(Formatting.None).Length > 1_000_000)
                throw new ArgumentException("A GitHub source record exceeds the 1 MB limit.");
        }

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var linked = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM product_github_repositories WHERE product_id=@product AND repository_url=@url)", connection, transaction))
        {
            linked.Parameters.AddWithValue("product", productId); linked.Parameters.AddWithValue("url", canonicalUrl);
            if (!(bool)(await linked.ExecuteScalarAsync() ?? false))
                throw new ArgumentException("The repository URL is not linked to this product.");
        }

        var incomingKeys = unique;
        foreach (var record in records.Where(item => item.EntityType != "repository"))
        {
            var parentType = ParentType(record.EntityType);
            if (incomingKeys.Contains((parentType, record.ParentExternalId!))) continue;
            await using var parent = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM github_source_records WHERE product_id=@product AND repository_url=@url AND entity_type=@type AND external_id=@parent)", connection, transaction);
            parent.Parameters.AddWithValue("product", productId); parent.Parameters.AddWithValue("url", canonicalUrl);
            parent.Parameters.AddWithValue("type", parentType); parent.Parameters.AddWithValue("parent", record.ParentExternalId!);
            if (!(bool)(await parent.ExecuteScalarAsync() ?? false))
                throw new ArgumentException($"The parent {parentType} record '{record.ParentExternalId}' is not present in this repository snapshot or stored data.");
        }

        const string sql = """
            INSERT INTO github_source_records(product_id,repository_url,entity_type,external_id,parent_entity_type,parent_external_id,record_data,source_updated_at,synced_at)
            VALUES(@product,@url,@type,@external,@parentType,@parent,@data::jsonb,@sourceUpdated,now())
            ON CONFLICT(product_id,repository_url,entity_type,external_id) DO UPDATE SET
                parent_entity_type=EXCLUDED.parent_entity_type,
                parent_external_id=EXCLUDED.parent_external_id,
                record_data=EXCLUDED.record_data,
                source_updated_at=EXCLUDED.source_updated_at,
                synced_at=now()
            """;
        foreach (var record in records)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("product", productId);
            command.Parameters.AddWithValue("url", canonicalUrl);
            command.Parameters.AddWithValue("type", record.EntityType);
            command.Parameters.AddWithValue("external", record.ExternalId);
            var parentType = record.EntityType == "repository" ? null : ParentType(record.EntityType);
            command.Parameters.AddWithValue("parentType", (object?)parentType ?? DBNull.Value);
            command.Parameters.AddWithValue("parent", (object?)record.ParentExternalId ?? DBNull.Value);
            command.Parameters.Add(new NpgsqlParameter("data", NpgsqlDbType.Jsonb) { Value = record.Data.ToString(Formatting.None) });
            command.Parameters.AddWithValue("sourceUpdated", (object?)ReadUpdatedAt(record.Data) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        return records.Count;
    }

    private static string ParentType(string entityType) => entityType == "workflow_run" ? "workflow" : "repository";

    public async Task<GitHubSourcePage> List(Guid productId, string repositoryUrl, string entityType, int limit, int offset)
    {
        var canonicalUrl = ValidateRepositoryUrl(repositoryUrl);
        if (!RequiredFields.ContainsKey(entityType)) throw new ArgumentException("Unsupported GitHub entity type.");
        if (limit is < 1 or > 500 || offset < 0) throw new ArgumentException("Page limit must be 1–500 and offset cannot be negative.");
        var records = new List<GitHubSourceRecordResponse>();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using (var count = new NpgsqlCommand("SELECT count(*) FROM github_source_records WHERE product_id=@product AND repository_url=@url AND entity_type=@type", connection))
        {
            count.Parameters.AddWithValue("product", productId); count.Parameters.AddWithValue("url", canonicalUrl); count.Parameters.AddWithValue("type", entityType);
            var total = Convert.ToInt32(await count.ExecuteScalarAsync());
            await using var command = new NpgsqlCommand("SELECT entity_type,external_id,parent_external_id,record_data::text,source_updated_at,synced_at FROM github_source_records WHERE product_id=@product AND repository_url=@url AND entity_type=@type ORDER BY external_id LIMIT @limit OFFSET @offset", connection);
            command.Parameters.AddWithValue("product", productId); command.Parameters.AddWithValue("url", canonicalUrl); command.Parameters.AddWithValue("type", entityType);
            command.Parameters.AddWithValue("limit", limit); command.Parameters.AddWithValue("offset", offset);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                records.Add(new GitHubSourceRecordResponse(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                    JsonConvert.DeserializeObject<JObject>(reader.GetString(3)) ?? new JObject(),
                    reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4), reader.GetFieldValue<DateTimeOffset>(5)));
            return new GitHubSourcePage(records, total, limit, offset);
        }
    }

    private static string ValidateRepositoryUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new ArgumentException("Repository URL must be an HTTPS github.com repository URL.");
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2 || segments.Any(segment => segment is "." or ".."))
            throw new ArgumentException("Repository URL must use the https://github.com/owner/repository format.");
        return $"https://github.com/{segments[0]}/{segments[1]}";
    }

    private static DateTimeOffset? ReadUpdatedAt(JObject data)
    {
        foreach (var key in new[] { "updatedAt", "createdAt", "commitDate", "publishedAt", "startedAt" })
            if (DateTimeOffset.TryParse(data[key]?.ToString(), out var value)) return value.ToUniversalTime();
        return null;
    }
}
