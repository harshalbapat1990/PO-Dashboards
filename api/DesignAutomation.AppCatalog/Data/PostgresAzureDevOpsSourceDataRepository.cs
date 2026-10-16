using System.Globalization;
using DesignAutomation.AppCatalog.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;

namespace DesignAutomation.AppCatalog.Data;

/// <summary>Validates and persists operator-supplied Azure DevOps snapshots.</summary>
public sealed class PostgresAzureDevOpsSourceDataRepository(NpgsqlDataSource dataSource) : IAzureDevOpsSourceDataRepository
{
    private static readonly Dictionary<string, string[]> RequiredFields = new(StringComparer.Ordinal)
    {
        ["project"] = ["projectId", "name", "state", "visibility", "description"],
        ["iteration"] = ["iterationId", "projectId", "name", "startDate", "endDate", "state"],
        ["work_item"] = ["workItemId", "projectId", "workItemType", "title", "description", "state", "reason", "assignedTo", "createdAt", "changedAt", "closedAt", "resolvedAt", "priority", "severity", "iterationId", "areaPath", "tags", "parentId", "children", "storyPoints", "effort", "originalEstimate", "remainingWork", "completedWork", "reopenCount"],
        ["pipeline"] = ["pipelineId", "projectId", "name", "folder", "revision", "url"],
        ["pipeline_run"] = ["runId", "pipelineId", "projectId", "branch", "status", "result", "startedAt", "completedAt", "durationSeconds", "trigger", "commit", "failureInformation"],
        ["test_plan"] = ["planId", "projectId", "name", "state", "startedAt", "completedAt"],
        ["test_suite"] = ["suiteId", "planId", "projectId", "name", "suiteType", "state", "testCaseIds"],
        ["test_case"] = ["caseId", "suiteId", "projectId", "title", "state", "priority", "steps"],
        ["test_run"] = ["runId", "planId", "projectId", "name", "state", "result", "startedAt", "completedAt", "durationSeconds", "passed", "failed", "blocked", "skipped"],
        ["test_result"] = ["resultId", "runId", "caseId", "projectId", "outcome", "durationSeconds", "startedAt", "completedAt", "errorMessage"]
    };

    private static readonly Dictionary<string, string> IdFields = new(StringComparer.Ordinal)
    {
        ["project"] = "projectId", ["iteration"] = "iterationId", ["work_item"] = "workItemId",
        ["pipeline"] = "pipelineId", ["pipeline_run"] = "runId", ["test_plan"] = "planId",
        ["test_suite"] = "suiteId", ["test_case"] = "caseId", ["test_run"] = "runId",
        ["test_result"] = "resultId"
    };

    private static readonly HashSet<(string Source, string Relation, string Target)> AllowedRelationships =
    [
        ("work_item", "parent", "work_item"),
        ("work_item", "iteration", "iteration"),
        ("pipeline_run", "pipeline", "pipeline"),
        ("test_suite", "test_plan", "test_plan"),
        ("test_suite", "parent", "test_suite"),
        ("test_case", "test_suite", "test_suite"),
        ("test_run", "test_plan", "test_plan"),
        ("test_run", "test_suite", "test_suite"),
        ("test_result", "test_run", "test_run"),
        ("test_result", "test_case", "test_case")
    ];

    public async Task<string?> GetOrganization(Guid productId, string projectId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT organization FROM integrations WHERE product_id=@product AND provider='AzureDevOps' AND repository_or_project=@project AND organization<>'' ORDER BY updated_at DESC LIMIT 1", connection);
        command.Parameters.AddWithValue("product", productId);
        command.Parameters.AddWithValue("project", projectId);
        return (await command.ExecuteScalarAsync()) as string;
    }

    public async Task<AzureDevOpsImportResult> Import(Guid productId, AzureDevOpsSnapshotImportRequest snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var projectId = RequiredText(snapshot.ProjectId, "Project ID", 200);
        if (snapshot.Records is null || snapshot.Records.Count is < 1 or > 20000)
            throw new ArgumentException("A snapshot must contain between 1 and 20000 records.");
        if ((snapshot.Relationships?.Count ?? 0) > 50000)
            throw new ArgumentException("A snapshot can contain at most 50000 relationships.");

        var recordKeys = new HashSet<(string Type, string Id)>();
        foreach (var record in snapshot.Records)
        {
            if (record is null || string.IsNullOrWhiteSpace(record.EntityType)
                || !RequiredFields.TryGetValue(record.EntityType, out var required))
                throw new ArgumentException("A snapshot record has an unsupported entityType.");
            var externalId = RequiredText(record.ExternalId, "Record externalId", 300);
            if (!string.Equals(externalId, record.ExternalId, StringComparison.Ordinal))
                throw new ArgumentException("Record externalId values must not contain leading or trailing whitespace.");
            if (!recordKeys.Add((record.EntityType, externalId)))
                throw new ArgumentException($"The snapshot contains duplicate {record.EntityType} ID '{externalId}'.");
            if (record.Data is null || required.Any(field => !record.Data.ContainsKey(field)))
                throw new ArgumentException($"The {record.EntityType} record '{externalId}' is missing required normalized fields.");
            if (!string.Equals(record.Data[IdFields[record.EntityType]]?.ToString(), externalId, StringComparison.Ordinal))
                throw new ArgumentException($"The {record.EntityType} record externalId must match its normalized identifier field.");
            if (!string.Equals(record.Data["projectId"]?.ToString(), projectId, StringComparison.Ordinal))
                throw new ArgumentException($"Every record must belong to linked project '{projectId}'.");
            if (record.Data.ToString(Formatting.None).Length > 1_000_000)
                throw new ArgumentException($"The {record.EntityType} record '{externalId}' exceeds the 1 MB limit.");
        }
        if (!recordKeys.Contains(("project", projectId)))
            throw new ArgumentException("The snapshot must include a project record whose ID matches projectId.");

        var relationships = snapshot.Relationships ?? [];
        var uniqueRelationships = new HashSet<(string, string, string, string, string)>();
        foreach (var relationship in relationships)
        {
            if (relationship is null
                || !AllowedRelationships.Contains((relationship.SourceEntityType, relationship.RelationshipType, relationship.TargetEntityType))
                || string.IsNullOrWhiteSpace(relationship.SourceExternalId)
                || string.IsNullOrWhiteSpace(relationship.TargetExternalId)
                || relationship.SourceExternalId.Length > 300
                || relationship.TargetExternalId.Length > 300
                || relationship.SourceExternalId != relationship.SourceExternalId.Trim()
                || relationship.TargetExternalId != relationship.TargetExternalId.Trim())
                throw new ArgumentException("A relationship has an invalid source, target, ID, or relationshipType.");
            if (!uniqueRelationships.Add((relationship.SourceEntityType, relationship.SourceExternalId, relationship.RelationshipType,
                    relationship.TargetEntityType, relationship.TargetExternalId)))
                throw new ArgumentException("The snapshot contains a duplicate relationship.");
        }
        ValidateDeclaredReferences(snapshot.Records, uniqueRelationships);

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var deleted = 0;
        await using (var linked = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM product_azure_projects WHERE product_id=@product AND project_id=@project)", connection, transaction))
        {
            linked.Parameters.AddWithValue("product", productId);
            linked.Parameters.AddWithValue("project", projectId);
            if (!(bool)(await linked.ExecuteScalarAsync() ?? false))
                throw new ArgumentException("The Azure DevOps project ID must already be linked to this product.");
        }

        if (snapshot.CompleteSnapshot)
        {
            await using var createIncoming = new NpgsqlCommand("CREATE TEMP TABLE incoming_azure_devops_records(entity_type text NOT NULL, external_id text NOT NULL, PRIMARY KEY(entity_type,external_id)) ON COMMIT DROP", connection, transaction);
            await createIncoming.ExecuteNonQueryAsync();
            foreach (var (type, id) in recordKeys)
            {
                await using var add = new NpgsqlCommand("INSERT INTO incoming_azure_devops_records VALUES(@type,@id)", connection, transaction);
                add.Parameters.AddWithValue("type", type); add.Parameters.AddWithValue("id", id);
                await add.ExecuteNonQueryAsync();
            }
            await using (var deleteRelations = new NpgsqlCommand("DELETE FROM azure_devops_source_relationships WHERE product_id=@product AND project_id=@project", connection, transaction))
            {
                deleteRelations.Parameters.AddWithValue("product", productId); deleteRelations.Parameters.AddWithValue("project", projectId);
                await deleteRelations.ExecuteNonQueryAsync();
            }
            await using (var countMissing = new NpgsqlCommand("SELECT count(*) FROM azure_devops_source_records old WHERE product_id=@product AND project_id=@project AND NOT EXISTS (SELECT 1 FROM incoming_azure_devops_records incoming WHERE incoming.entity_type=old.entity_type AND incoming.external_id=old.external_id)", connection, transaction))
            {
                countMissing.Parameters.AddWithValue("product", productId); countMissing.Parameters.AddWithValue("project", projectId);
                deleted = Convert.ToInt32(await countMissing.ExecuteScalarAsync());
            }
            await using var deleteMissing = new NpgsqlCommand("DELETE FROM azure_devops_source_records old WHERE product_id=@product AND project_id=@project AND NOT EXISTS (SELECT 1 FROM incoming_azure_devops_records incoming WHERE incoming.entity_type=old.entity_type AND incoming.external_id=old.external_id)", connection, transaction);
            deleteMissing.Parameters.AddWithValue("product", productId); deleteMissing.Parameters.AddWithValue("project", projectId);
            await deleteMissing.ExecuteNonQueryAsync();
        }
        else
        {
            foreach (var record in snapshot.Records)
            {
                await using var removeOutgoing = new NpgsqlCommand("DELETE FROM azure_devops_source_relationships WHERE product_id=@product AND project_id=@project AND source_entity_type=@type AND source_external_id=@id", connection, transaction);
                removeOutgoing.Parameters.AddWithValue("product", productId); removeOutgoing.Parameters.AddWithValue("project", projectId);
                removeOutgoing.Parameters.AddWithValue("type", record.EntityType); removeOutgoing.Parameters.AddWithValue("id", record.ExternalId);
                await removeOutgoing.ExecuteNonQueryAsync();
            }
        }

        foreach (var record in snapshot.Records)
        {
            const string sql = """
                INSERT INTO azure_devops_source_records(product_id,project_id,entity_type,external_id,record_data,source_updated_at,synced_at)
                VALUES(@product,@project,@type,@external,@data::jsonb,@updated,now())
                ON CONFLICT(product_id,project_id,entity_type,external_id) DO UPDATE SET
                    record_data=EXCLUDED.record_data, source_updated_at=EXCLUDED.source_updated_at, synced_at=now()
                """;
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("product", productId); command.Parameters.AddWithValue("project", projectId);
            command.Parameters.AddWithValue("type", record.EntityType); command.Parameters.AddWithValue("external", record.ExternalId);
            command.Parameters.Add(new NpgsqlParameter("data", NpgsqlDbType.Jsonb) { Value = record.Data.ToString(Formatting.None) });
            command.Parameters.AddWithValue("updated", (object?)ReadUpdatedAt(record.Data) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
        }

        foreach (var relationship in relationships)
        {
            if (!recordKeys.Contains((relationship.SourceEntityType, relationship.SourceExternalId))
                && !await Exists(connection, transaction, productId, projectId, relationship.SourceEntityType, relationship.SourceExternalId))
                throw new ArgumentException($"Relationship source '{relationship.SourceEntityType}/{relationship.SourceExternalId}' is not present in this project.");
            if (!recordKeys.Contains((relationship.TargetEntityType, relationship.TargetExternalId))
                && !await Exists(connection, transaction, productId, projectId, relationship.TargetEntityType, relationship.TargetExternalId))
                throw new ArgumentException($"Relationship target '{relationship.TargetEntityType}/{relationship.TargetExternalId}' is not present in this project.");

            await using var add = new NpgsqlCommand("""
                INSERT INTO azure_devops_source_relationships(product_id,project_id,source_entity_type,source_external_id,relationship_type,target_entity_type,target_external_id)
                VALUES(@product,@project,@sourceType,@sourceId,@relation,@targetType,@targetId)
                ON CONFLICT DO NOTHING
                """, connection, transaction);
            add.Parameters.AddWithValue("product", productId); add.Parameters.AddWithValue("project", projectId);
            add.Parameters.AddWithValue("sourceType", relationship.SourceEntityType); add.Parameters.AddWithValue("sourceId", relationship.SourceExternalId);
            add.Parameters.AddWithValue("relation", relationship.RelationshipType); add.Parameters.AddWithValue("targetType", relationship.TargetEntityType);
            add.Parameters.AddWithValue("targetId", relationship.TargetExternalId);
            await add.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return new AzureDevOpsImportResult(projectId, snapshot.Records.Count, snapshot.Records.Count, deleted, relationships.Count);
    }

    public async Task<AzureDevOpsSourcePage> List(Guid productId, string projectId, string entityType, int limit, int offset)
    {
        _ = RequiredText(projectId, "Project ID", 200);
        if (!RequiredFields.ContainsKey(entityType)) throw new ArgumentException("Unsupported Azure DevOps entityType.");
        if (limit is < 1 or > 500 || offset < 0) throw new ArgumentException("Page limit must be 1–500 and offset cannot be negative.");
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var count = new NpgsqlCommand("SELECT count(*) FROM azure_devops_source_records WHERE product_id=@product AND project_id=@project AND entity_type=@type", connection);
        count.Parameters.AddWithValue("product", productId); count.Parameters.AddWithValue("project", projectId); count.Parameters.AddWithValue("type", entityType);
        var total = Convert.ToInt32(await count.ExecuteScalarAsync());
        await using var command = new NpgsqlCommand("SELECT entity_type,external_id,record_data::text,source_updated_at,synced_at FROM azure_devops_source_records WHERE product_id=@product AND project_id=@project AND entity_type=@type ORDER BY external_id LIMIT @limit OFFSET @offset", connection);
        command.Parameters.AddWithValue("product", productId); command.Parameters.AddWithValue("project", projectId); command.Parameters.AddWithValue("type", entityType);
        command.Parameters.AddWithValue("limit", limit); command.Parameters.AddWithValue("offset", offset);
        var items = new List<AzureDevOpsSourceRecordResponse>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(new(reader.GetString(0), reader.GetString(1), JsonConvert.DeserializeObject<JObject>(reader.GetString(2)) ?? new JObject(),
                reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3), reader.GetFieldValue<DateTimeOffset>(4)));
        return new AzureDevOpsSourcePage(items, total, limit, offset);
    }

    private static async Task<bool> Exists(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid productId, string projectId, string type, string id)
    {
        await using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM azure_devops_source_records WHERE product_id=@product AND project_id=@project AND entity_type=@type AND external_id=@id)", connection, transaction);
        command.Parameters.AddWithValue("product", productId); command.Parameters.AddWithValue("project", projectId);
        command.Parameters.AddWithValue("type", type); command.Parameters.AddWithValue("id", id);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private static string RequiredText(string? value, string label, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException($"{label} is required and must be at most {maxLength} characters.");
        return value.Trim();
    }

    private static DateTimeOffset? ReadUpdatedAt(JObject data)
    {
        foreach (var key in new[] { "changedAt", "updatedAt", "completedAt", "createdAt", "startedAt" })
            if (DateTimeOffset.TryParse(data[key]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value))
                return value.ToUniversalTime();
        return null;
    }

    private static void ValidateDeclaredReferences(
        IReadOnlyCollection<AzureDevOpsSnapshotRecord> records,
        HashSet<(string SourceType, string SourceId, string Relation, string TargetType, string TargetId)> relationships)
    {
        foreach (var record in records)
        {
            var data = record.Data;
            var references = record.EntityType switch
            {
                "work_item" => new[]
                {
                    ("parentId", "parent", "work_item"),
                    ("iterationId", "iteration", "iteration")
                },
                "pipeline_run" => new[] { ("pipelineId", "pipeline", "pipeline") },
                "test_suite" => new[] { ("planId", "test_plan", "test_plan") },
                "test_case" => new[] { ("suiteId", "test_suite", "test_suite") },
                "test_run" => new[] { ("planId", "test_plan", "test_plan") },
                "test_result" => new[]
                {
                    ("runId", "test_run", "test_run"),
                    ("caseId", "test_case", "test_case")
                },
                _ => Array.Empty<(string, string, string)>()
            };
            foreach (var (field, relation, targetType) in references)
            {
                var targetId = data[field]?.ToString();
                if (string.IsNullOrWhiteSpace(targetId)) continue;
                if (!relationships.Contains((record.EntityType, record.ExternalId, relation, targetType, targetId)))
                    throw new ArgumentException($"Record {record.EntityType}/{record.ExternalId} declares {field} '{targetId}' without the matching '{relation}' relationship.");
            }
        }
    }
}
