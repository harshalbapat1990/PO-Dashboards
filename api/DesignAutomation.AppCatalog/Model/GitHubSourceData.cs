using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DesignAutomation.AppCatalog.Model;

public sealed record GitHubSnapshotImportRequest
{
    [JsonProperty("repositoryUrl")] public string RepositoryUrl { get; init; } = string.Empty;
    [JsonProperty("records")] public List<GitHubSnapshotRecord> Records { get; init; } = [];
}

public sealed record GitHubSyncRequest([property: JsonProperty("repositoryUrl")] string RepositoryUrl);

public sealed record GitHubSnapshotRecord
{
    [JsonProperty("entityType")] public string EntityType { get; init; } = string.Empty;
    [JsonProperty("externalId")] public string ExternalId { get; init; } = string.Empty;
    [JsonProperty("parentExternalId")] public string? ParentExternalId { get; init; }
    [JsonProperty("data")] public JObject Data { get; init; } = new();
}

public sealed record GitHubSourceRecordResponse(
    [property: JsonProperty("entityType")] string EntityType,
    [property: JsonProperty("externalId")] string ExternalId,
    [property: JsonProperty("parentExternalId")] string? ParentExternalId,
    [property: JsonProperty("data")] JObject Data,
    [property: JsonProperty("sourceUpdatedAt")] DateTimeOffset? SourceUpdatedAt,
    [property: JsonProperty("syncedAt")] DateTimeOffset SyncedAt);

public sealed record GitHubImportResult(
    [property: JsonProperty("repositoryUrl")] string RepositoryUrl,
    [property: JsonProperty("recordsReceived")] int RecordsReceived,
    [property: JsonProperty("recordsUpserted")] int RecordsUpserted);

public sealed record GitHubSourcePage(
    [property: JsonProperty("items")] List<GitHubSourceRecordResponse> Items,
    [property: JsonProperty("total")] int Total,
    [property: JsonProperty("limit")] int Limit,
    [property: JsonProperty("offset")] int Offset);

public interface IGitHubSourceDataRepository
{
    Task<int> Import(Guid productId, string repositoryUrl, IReadOnlyCollection<GitHubSnapshotRecord> records);
    Task<GitHubSourcePage> List(Guid productId, string repositoryUrl, string entityType, int limit, int offset);
}

public interface IGitHubSnapshotSource
{
    Task<GitHubSnapshotImportRequest> FetchRepository(string repositoryUrl, CancellationToken cancellationToken = default);
}
