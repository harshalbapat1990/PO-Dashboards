using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using DesignAutomation.AppCatalog.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DesignAutomation.AppCatalog.Data;

/// <summary>
/// Explicit Azure DevOps Services reader. It is registered only outside Local
/// mode when a server-side PAT is configured. It never logs credentials.
/// </summary>
public sealed class AzureDevOpsRestSnapshotSource(HttpClient httpClient) : IAzureDevOpsSnapshotSource
{
    private const int MaxPages = 500;
    private const int MaxWorkItems = 20000;

    public async Task<AzureDevOpsSnapshotImportRequest> FetchProject(string organization, string projectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organization) || organization.Length > 100
            || organization.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and '_'))
            throw new ArgumentException("Azure DevOps organization must be its short organization name.");
        if (string.IsNullOrWhiteSpace(projectId) || projectId.Length > 200)
            throw new ArgumentException("A linked Azure DevOps project ID is required.");

        var orgRoot = $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/_apis";
        var root = $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/{Uri.EscapeDataString(projectId)}/_apis";
        var project = await GetObject($"{orgRoot}/projects/{Uri.EscapeDataString(projectId)}?api-version=7.1", cancellationToken);
        var apiProjectId = String(project, "id") ?? projectId;
        if (!string.Equals(apiProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Azure DevOps returned a project ID different from the linked project ID.");
        // Preserve the exact linked identifier as the PostgreSQL project key.
        var resolvedId = projectId;

        var records = new List<AzureDevOpsSnapshotRecord>();
        var relationships = new List<AzureDevOpsSnapshotRelationship>();
        var projectName = String(project, "name") ?? string.Empty;
        var projectData = new JObject
        {
            ["projectId"] = resolvedId,
            ["name"] = projectName,
            ["state"] = String(project, "state") ?? "unknown",
            ["visibility"] = String(project, "visibility") ?? "unknown",
            ["description"] = String(project, "description") ?? string.Empty
        };
        records.Add(Record("project", resolvedId, projectData));

        var iterationsByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var teams = await GetAll($"{orgRoot}/projects/{Uri.EscapeDataString(projectId)}/teams?api-version=7.1", cancellationToken);
        foreach (var team in teams)
        {
            var teamId = String(team, "id");
            if (string.IsNullOrWhiteSpace(teamId)) continue;
            var teamIterations = await GetAll($"https://dev.azure.com/{Uri.EscapeDataString(organization)}/{Uri.EscapeDataString(projectId)}/{Uri.EscapeDataString(teamId)}/_apis/work/teamsettings/iterations?$timeframe=all&api-version=7.1", cancellationToken);
            foreach (var iteration in teamIterations)
            {
                var id = String(iteration, "id");
                if (string.IsNullOrWhiteSpace(id)) continue;
                var path = String(iteration, "path") ?? String(iteration, "name") ?? id;
                iterationsByPath[path] = id;
                var data = new JObject
                {
                    ["iterationId"] = id, ["projectId"] = resolvedId,
                    ["name"] = String(iteration, "name") ?? path,
                    ["startDate"] = String(iteration, "attributes", "startDate"),
                    ["endDate"] = String(iteration, "attributes", "finishDate"),
                    ["state"] = String(iteration, "attributes", "timeFrame") ?? "unknown",
                    ["path"] = path
                };
                UpsertRecord(records, Record("iteration", id, data));
            }
        }

        var wiql = new JObject { ["query"] = "SELECT [System.Id] FROM WorkItems ORDER BY [System.Id]" };
        var query = await PostObject($"{root}/wit/wiql?$top={MaxWorkItems + 1}&api-version=7.1", wiql, cancellationToken);
        var workItemIds = (query["workItems"] as JArray ?? new JArray())
            .Select(item => String(item, "id"))
            .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).Take(MaxWorkItems + 1).ToList();
        if (workItemIds.Count > MaxWorkItems)
            throw new InvalidOperationException($"Azure DevOps project exceeds the {MaxWorkItems} work-item import limit.");

        var workItems = new Dictionary<string, JObject>(StringComparer.Ordinal);
        var fields = new JArray
        {
            "System.Id", "System.WorkItemType", "System.Title", "System.Description", "System.State", "System.Reason",
            "System.AssignedTo", "System.CreatedDate", "System.ChangedDate", "System.ClosedDate", "Microsoft.VSTS.Common.ResolvedDate",
            "Microsoft.VSTS.Common.Priority", "Microsoft.VSTS.Common.Severity", "System.IterationPath", "System.AreaPath", "System.Tags",
            "System.Parent", "Microsoft.VSTS.Scheduling.StoryPoints", "Microsoft.VSTS.Scheduling.Effort",
            "Microsoft.VSTS.Scheduling.OriginalEstimate", "Microsoft.VSTS.Scheduling.RemainingWork", "Microsoft.VSTS.Scheduling.CompletedWork",
            "Microsoft.VSTS.Common.ReopenCount"
        };
        foreach (var batch in workItemIds.Chunk(200))
        {
            var response = await PostObject($"{root}/wit/workitemsbatch?api-version=7.1", new JObject
            {
                ["ids"] = new JArray(batch.Select(id => (JToken)id!)), ["fields"] = fields.DeepClone(), ["$expand"] = "Relations"
            }, cancellationToken);
            foreach (var item in response["value"] as JArray ?? new JArray())
                if (item is JObject workItem && String(workItem, "id") is { } id) workItems[id] = workItem;
        }

        var childIds = workItems.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var (id, item) in workItems)
        {
            var dataFields = item["fields"] as JObject ?? new JObject();
            var parentId = dataFields["System.Parent"]?.ToString()
                ?? (item["relations"] as JArray ?? new JArray())
                    .Where(link => String(link, "rel") == "System.LinkTypes.Hierarchy-Reverse")
                    .Select(link => String(link, "url"))
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => url!.TrimEnd('/').Split('/').Last())
                    .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(parentId) && workItems.ContainsKey(parentId))
            {
                relationships.Add(Relationship("work_item", id, "parent", "work_item", parentId));
                childIds[parentId].Add(id);
            }
            var iterationPath = dataFields["System.IterationPath"]?.ToString();
            var iterationId = iterationsByPath.GetValueOrDefault(iterationPath ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(iterationPath) && iterationId is null
                && !string.Equals(iterationPath, projectName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Work item {id} references iteration '{iterationPath}', but that iteration was not returned. Verify team visibility and iteration access; refusing to import an incomplete snapshot.");
            if (iterationId is not null) relationships.Add(Relationship("work_item", id, "iteration", "iteration", iterationId));

            var type = dataFields["System.WorkItemType"]?.ToString() ?? "Work Item";
            var reopened = Integer(dataFields, "Microsoft.VSTS.Common.ReopenCount") ?? 0;
            var data = new JObject
            {
                ["workItemId"] = id, ["projectId"] = resolvedId, ["workItemType"] = type,
                ["title"] = dataFields["System.Title"] ?? JValue.CreateString(string.Empty),
                ["description"] = dataFields["System.Description"] ?? JValue.CreateString(string.Empty),
                ["state"] = dataFields["System.State"] ?? JValue.CreateString("unknown"),
                ["reason"] = dataFields["System.Reason"], ["assignedTo"] = Identity(dataFields["System.AssignedTo"]),
                ["createdAt"] = dataFields["System.CreatedDate"], ["changedAt"] = dataFields["System.ChangedDate"],
                ["closedAt"] = dataFields["System.ClosedDate"], ["resolvedAt"] = dataFields["Microsoft.VSTS.Common.ResolvedDate"],
                ["priority"] = dataFields["Microsoft.VSTS.Common.Priority"], ["severity"] = dataFields["Microsoft.VSTS.Common.Severity"],
                ["iterationId"] = iterationId, ["areaPath"] = dataFields["System.AreaPath"],
                ["tags"] = SplitTags(dataFields["System.Tags"]?.ToString()), ["parentId"] = parentId,
                ["children"] = new JArray(), ["storyPoints"] = dataFields["Microsoft.VSTS.Scheduling.StoryPoints"],
                ["effort"] = dataFields["Microsoft.VSTS.Scheduling.Effort"],
                ["originalEstimate"] = dataFields["Microsoft.VSTS.Scheduling.OriginalEstimate"],
                ["remainingWork"] = dataFields["Microsoft.VSTS.Scheduling.RemainingWork"],
                ["completedWork"] = dataFields["Microsoft.VSTS.Scheduling.CompletedWork"], ["reopenCount"] = reopened
            };
            UpsertRecord(records, Record("work_item", id, data));
        }
        foreach (var record in records.Where(record => record.EntityType == "work_item"))
            record.Data["children"] = new JArray(childIds.GetValueOrDefault(record.ExternalId) ?? []);

        await AddPipelines(root, resolvedId, records, relationships, cancellationToken);
        await AddTests(root, resolvedId, records, relationships, cancellationToken);
        return new AzureDevOpsSnapshotImportRequest
        {
            ProjectId = resolvedId,
            CompleteSnapshot = true,
            Records = records,
            Relationships = relationships
        };
    }

    private async Task AddPipelines(string root, string projectId, List<AzureDevOpsSnapshotRecord> records,
        List<AzureDevOpsSnapshotRelationship> relationships, CancellationToken cancellationToken)
    {
        var definitions = await GetAll($"{root}/pipelines?api-version=7.1", cancellationToken);
        foreach (var pipeline in definitions)
        {
            var id = String(pipeline, "id"); if (string.IsNullOrWhiteSpace(id)) continue;
            records.Add(Record("pipeline", id, new JObject
            {
                ["pipelineId"] = id, ["projectId"] = projectId, ["name"] = String(pipeline, "name") ?? id,
                ["folder"] = String(pipeline, "folder") ?? string.Empty, ["revision"] = pipeline["revision"], ["url"] = String(pipeline, "url")
            }));
            var runs = await GetAll($"{root}/pipelines/{Uri.EscapeDataString(id)}/runs?api-version=7.1", cancellationToken);
            foreach (var run in runs)
            {
                var runId = String(run, "id"); if (string.IsNullOrWhiteSpace(runId)) continue;
                var started = String(run, "createdDate"); var completed = String(run, "finishedDate");
                records.Add(Record("pipeline_run", runId, new JObject
                {
                    ["runId"] = runId, ["pipelineId"] = id, ["projectId"] = projectId,
                    ["branch"] = String(run["resources"]?["repositories"]?["self"], "refName") ?? string.Empty,
                    ["status"] = String(run, "state") ?? "unknown", ["result"] = String(run, "result"),
                    ["startedAt"] = started, ["completedAt"] = completed,
                    ["durationSeconds"] = DurationSeconds(started, completed),
                    ["trigger"] = String(run, "reason") ?? string.Empty,
                    ["commit"] = String(run["resources"]?["repositories"]?["self"], "version"),
                    ["failureInformation"] = String(run, "result") == "failed" ? String(run, "state") : null
                }));
                relationships.Add(Relationship("pipeline_run", runId, "pipeline", "pipeline", id));
            }
        }
    }

    private async Task AddTests(string root, string projectId, List<AzureDevOpsSnapshotRecord> records,
        List<AzureDevOpsSnapshotRelationship> relationships, CancellationToken cancellationToken)
    {
        var plans = await GetAll($"{root}/testplan/plans?api-version=7.1", cancellationToken);
        foreach (var plan in plans)
        {
            var planId = String(plan, "id"); if (string.IsNullOrWhiteSpace(planId)) continue;
            records.Add(Record("test_plan", planId, new JObject
            {
                ["planId"] = planId, ["projectId"] = projectId, ["name"] = String(plan, "name") ?? planId,
                ["state"] = String(plan, "state") ?? "unknown", ["startedAt"] = String(plan, "startDate"), ["completedAt"] = String(plan, "endDate")
            }));
            var suites = await GetAll($"{root}/testplan/Plans/{Uri.EscapeDataString(planId)}/Suites?api-version=7.1", cancellationToken);
            foreach (var suite in suites)
            {
                var suiteId = String(suite, "id"); if (string.IsNullOrWhiteSpace(suiteId)) continue;
                var testCaseIds = (suite["testCases"] as JArray ?? new JArray()).Select(testCase => String(testCase["workItem"], "id")).Where(id => id is not null).ToArray();
                records.Add(Record("test_suite", suiteId, new JObject
                {
                    ["suiteId"] = suiteId, ["planId"] = planId, ["projectId"] = projectId,
                    ["name"] = String(suite, "name") ?? suiteId, ["suiteType"] = String(suite, "suiteType") ?? "unknown",
                    ["state"] = String(suite, "state") ?? "unknown", ["testCaseIds"] = new JArray(testCaseIds.Where(id => id is not null).Select(id => (JToken)id!))
                }));
                relationships.Add(Relationship("test_suite", suiteId, "test_plan", "test_plan", planId));
                var cases = await GetAll($"{root}/testplan/Plans/{Uri.EscapeDataString(planId)}/Suites/{Uri.EscapeDataString(suiteId)}/TestCase?api-version=7.1", cancellationToken);
                foreach (var testCase in cases)
                {
                    var caseId = String(testCase["workItem"], "id") ?? String(testCase, "id");
                    if (string.IsNullOrWhiteSpace(caseId)) continue;
                    var workItem = testCase["workItem"] as JObject ?? testCase;
                    UpsertRecord(records, Record("test_case", caseId, new JObject
                    {
                        ["caseId"] = caseId, ["suiteId"] = suiteId, ["projectId"] = projectId,
                        ["title"] = String(workItem, "name") ?? String(workItem, "title") ?? caseId,
                        ["state"] = String(workItem, "state") ?? "unknown", ["priority"] = workItem["priority"],
                        ["steps"] = workItem["steps"] ?? new JArray()
                    }));
                    relationships.Add(Relationship("test_case", caseId, "test_suite", "test_suite", suiteId));
                }
            }
        }

        var runs = await GetAll($"{root}/test/runs?includeRunDetails=true&$top=1000&api-version=7.1", cancellationToken);
        foreach (var run in runs)
        {
            var runId = String(run, "id"); if (string.IsNullOrWhiteSpace(runId)) continue;
            var planId = String(run["plan"], "id") ?? "unknown";
            var started = String(run, "startedDate"); var completed = String(run, "completedDate");
            var resultData = await GetAll($"{root}/test/Runs/{Uri.EscapeDataString(runId)}/results?$top=1000&api-version=7.1", cancellationToken);
            var counts = resultData.GroupBy(item => String(item, "outcome") ?? "unknown", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            records.Add(Record("test_run", runId, new JObject
            {
                ["runId"] = runId, ["planId"] = planId, ["projectId"] = projectId,
                ["name"] = String(run, "name") ?? runId, ["state"] = String(run, "state") ?? "unknown",
                ["result"] = counts.GetValueOrDefault("failed") > 0 ? "failed" : "passed",
                ["startedAt"] = started, ["completedAt"] = completed, ["durationSeconds"] = DurationSeconds(started, completed),
                ["passed"] = counts.GetValueOrDefault("passed"), ["failed"] = counts.GetValueOrDefault("failed"),
                ["blocked"] = counts.GetValueOrDefault("blocked"), ["skipped"] = counts.GetValueOrDefault("notapplicable")
            }));
            if (planId != "unknown" && records.Any(record => record.EntityType == "test_plan" && record.ExternalId == planId))
                relationships.Add(Relationship("test_run", runId, "test_plan", "test_plan", planId));
            foreach (var result in resultData)
            {
                var resultId = String(result, "id"); var caseId = String(result["testCase"], "id");
                if (string.IsNullOrWhiteSpace(resultId) || string.IsNullOrWhiteSpace(caseId)) continue;
                var duration = Integer(result, "durationInMs");
                UpsertRecord(records, Record("test_result", resultId, new JObject
                {
                    ["resultId"] = resultId, ["runId"] = runId, ["caseId"] = caseId, ["projectId"] = projectId,
                    ["outcome"] = String(result, "outcome") ?? "unknown",
                    ["durationSeconds"] = duration is null ? null : duration.Value / 1000.0,
                    ["startedAt"] = String(result, "startedDate"), ["completedAt"] = String(result, "completedDate"),
                    ["errorMessage"] = String(result, "errorMessage")
                }));
                relationships.Add(Relationship("test_result", resultId, "test_run", "test_run", runId));
                if (records.Any(record => record.EntityType == "test_case" && record.ExternalId == caseId))
                    relationships.Add(Relationship("test_result", resultId, "test_case", "test_case", caseId));
            }
        }
    }

    private async Task<JArray> GetAll(string url, CancellationToken cancellationToken)
    {
        var all = new JArray(); var next = url; var offset = 0;
        for (var page = 0; !string.IsNullOrWhiteSpace(next); page++)
        {
            if (page >= MaxPages) throw new InvalidOperationException("Azure DevOps pagination exceeded the 500-page safety limit.");
            using var response = await Send(HttpMethod.Get, next, null, cancellationToken);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var pageItems = body["value"] as JArray ?? body["projects"] as JArray;
            if (pageItems is null)
                throw new InvalidOperationException("Azure DevOps returned a collection response without a value array; refusing to treat it as an empty dataset.");
            all.Merge(pageItems);
            var pageCount = pageItems.Count;
            if (response.Headers.TryGetValues("x-ms-continuationtoken", out var continuation)
                && !string.IsNullOrWhiteSpace(continuation.FirstOrDefault()))
                next = AddQuery(url, "continuationToken", continuation.First());
            else if (pageCount >= 1000)
            {
                offset += pageCount;
                next = AddQuery(url, "$skip", offset.ToString(CultureInfo.InvariantCulture));
            }
            else next = string.Empty;
        }
        return all;
    }

    private async Task<JObject> GetObject(string url, CancellationToken cancellationToken)
    {
        using var response = await Send(HttpMethod.Get, url, null, cancellationToken);
        return JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private async Task<JObject> PostObject(string url, JObject body, CancellationToken cancellationToken)
    {
        using var response = await Send(HttpMethod.Post, url,
            new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json"), cancellationToken);
        return JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string url, HttpContent? content, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || !uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Azure DevOps returned an invalid API URL.");
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, uri) { Content = content is null ? null : new StringContent(await content.ReadAsStringAsync(cancellationToken), Encoding.UTF8, "application/json") };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode) return response;
            var retryable = response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;
            if (retryable && attempt < 2)
            {
                TimeSpan? retryDateDelay = response.Headers.RetryAfter?.Date is DateTimeOffset retryDate
                    ? retryDate - DateTimeOffset.UtcNow : null;
                var delay = response.Headers.RetryAfter?.Delta ?? retryDateDelay ?? TimeSpan.FromSeconds(attempt + 1);
                response.Dispose();
                if (delay > TimeSpan.FromSeconds(30)) throw new InvalidOperationException("Azure DevOps requested a retry delay over 30 seconds; retry the sync later.");
                if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
                await Task.Delay(delay, cancellationToken); continue;
            }
            var status = response.StatusCode; var requestId = response.Headers.TryGetValues("X-VSS-E2EID", out var ids) ? ids.FirstOrDefault() : "unknown";
            response.Dispose();
            throw new InvalidOperationException($"Azure DevOps API request to '{uri.AbsolutePath}{uri.Query}' failed with HTTP {(int)status} (request {requestId}). Verify project access and PAT scopes; credentials are not included in errors.");
        }
    }

    private static string AddQuery(string url, string key, string value)
        => url + (url.Contains('?') ? "&" : "?") + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);

    private static AzureDevOpsSnapshotRecord Record(string type, string id, JObject data)
        => new() { EntityType = type, ExternalId = id, Data = data };
    private static AzureDevOpsSnapshotRelationship Relationship(string sourceType, string sourceId, string relation, string targetType, string targetId)
        => new() { SourceEntityType = sourceType, SourceExternalId = sourceId, RelationshipType = relation, TargetEntityType = targetType, TargetExternalId = targetId };
    private static void UpsertRecord(List<AzureDevOpsSnapshotRecord> records, AzureDevOpsSnapshotRecord record)
    {
        var existing = records.FindIndex(item => item.EntityType == record.EntityType && item.ExternalId == record.ExternalId);
        if (existing < 0) records.Add(record); else records[existing] = record;
    }
    private static string? String(JToken? token, params string[] path)
    {
        foreach (var segment in path) token = token?[segment];
        if (token is null || token.Type is JTokenType.Null or JTokenType.Undefined) return null;
        if (token is JObject obj && obj["displayName"] is not null) return obj["displayName"]!.ToString();
        return token.ToString();
    }
    private static int? Integer(JToken? token, string field)
        => int.TryParse(token?[field]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    private static JArray SplitTags(string? value)
        => new((value ?? string.Empty).Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(tag => (JToken)tag));
    private static JToken Identity(JToken? value)
        => value is JObject obj ? new JObject { ["displayName"] = obj["displayName"], ["uniqueName"] = obj["uniqueName"] } : value?.DeepClone() ?? JValue.CreateNull();
    private static double? DurationSeconds(string? started, string? completed)
        => DateTimeOffset.TryParse(started, out var start) && DateTimeOffset.TryParse(completed, out var end)
            ? Math.Max(0, (end - start).TotalSeconds) : null;
}
