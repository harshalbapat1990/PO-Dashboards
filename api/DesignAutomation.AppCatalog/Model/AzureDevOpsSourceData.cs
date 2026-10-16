using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>Locally supplied, normalized Azure DevOps project snapshot.</summary>
public sealed record AzureDevOpsSnapshotImportRequest
{
    [JsonProperty("projectId")] public string ProjectId { get; init; } = string.Empty;
    [JsonProperty("completeSnapshot")] public bool CompleteSnapshot { get; init; }
    [JsonProperty("records")] public List<AzureDevOpsSnapshotRecord> Records { get; init; } = [];
    [JsonProperty("relationships")] public List<AzureDevOpsSnapshotRelationship> Relationships { get; init; } = [];
}

public sealed record AzureDevOpsSnapshotRecord
{
    [JsonProperty("entityType")] public string EntityType { get; init; } = string.Empty;
    [JsonProperty("externalId")] public string ExternalId { get; init; } = string.Empty;
    [JsonProperty("data")] public JObject Data { get; init; } = new();
}

public sealed record AzureDevOpsSnapshotRelationship
{
    [JsonProperty("sourceEntityType")] public string SourceEntityType { get; init; } = string.Empty;
    [JsonProperty("sourceExternalId")] public string SourceExternalId { get; init; } = string.Empty;
    [JsonProperty("relationshipType")] public string RelationshipType { get; init; } = string.Empty;
    [JsonProperty("targetEntityType")] public string TargetEntityType { get; init; } = string.Empty;
    [JsonProperty("targetExternalId")] public string TargetExternalId { get; init; } = string.Empty;
}

public sealed record AzureDevOpsSourceRecordResponse(
    [property: JsonProperty("entityType")] string EntityType,
    [property: JsonProperty("externalId")] string ExternalId,
    [property: JsonProperty("data")] JObject Data,
    [property: JsonProperty("sourceUpdatedAt")] DateTimeOffset? SourceUpdatedAt,
    [property: JsonProperty("syncedAt")] DateTimeOffset SyncedAt);

public sealed record AzureDevOpsSourcePage(
    [property: JsonProperty("items")] List<AzureDevOpsSourceRecordResponse> Items,
    [property: JsonProperty("total")] int Total,
    [property: JsonProperty("limit")] int Limit,
    [property: JsonProperty("offset")] int Offset);

public sealed record AzureDevOpsImportResult(
    [property: JsonProperty("projectId")] string ProjectId,
    [property: JsonProperty("recordsReceived")] int RecordsReceived,
    [property: JsonProperty("recordsUpserted")] int RecordsUpserted,
    [property: JsonProperty("recordsDeleted")] int RecordsDeleted,
    [property: JsonProperty("relationshipsReceived")] int RelationshipsReceived);

public sealed record AzureDevOpsSyncRequest([property: JsonProperty("projectId")] string ProjectId);

public interface IAzureDevOpsSnapshotSource
{
    Task<AzureDevOpsSnapshotImportRequest> FetchProject(string organization, string projectId,
        CancellationToken cancellationToken = default);
}

public interface IAzureDevOpsSourceDataRepository
{
    Task<string?> GetOrganization(Guid productId, string projectId);
    Task<AzureDevOpsImportResult> Import(Guid productId, AzureDevOpsSnapshotImportRequest snapshot);
    Task<AzureDevOpsSourcePage> List(Guid productId, string projectId, string entityType, int limit, int offset);
}
