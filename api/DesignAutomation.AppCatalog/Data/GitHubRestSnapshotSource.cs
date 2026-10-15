using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using DesignAutomation.AppCatalog.Model;
using Newtonsoft.Json.Linq;

namespace DesignAutomation.AppCatalog.Data;

/// <summary>
/// GitHub REST v3 snapshot reader. Registered only outside Local mode when a
/// server-side token is configured. It never logs request headers or tokens.
/// </summary>
public sealed class GitHubRestSnapshotSource(HttpClient httpClient) : IGitHubSnapshotSource
{
    private const string ApiRoot = "https://api.github.com";
    private const int MaxPages = 500;

    public async Task<GitHubSnapshotImportRequest> FetchRepository(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        var (owner, name, canonicalUrl) = ParseRepositoryUrl(repositoryUrl);
        var root = $"{ApiRoot}/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}";
        var repository = await GetObject(root, cancellationToken);
        var repositoryId = Text(repository, "id") ?? throw new InvalidOperationException("GitHub repository response did not include an id.");
        var defaultBranch = Text(repository, "default_branch") ?? string.Empty;
        var languages = await GetObject($"{root}/languages", cancellationToken);
        var records = new List<GitHubSnapshotRecord>
        {
            Record("repository", repositoryId, null, new
            {
                repositoryId,
                name = Text(repository, "name"),
                fullName = Text(repository, "full_name"),
                description = Text(repository, "description"),
                repositoryUrl = canonicalUrl,
                visibility = Text(repository, "visibility") ?? (Bool(repository, "private") ? "private" : "public"),
                defaultBranch,
                status = Bool(repository, "archived") ? "archived" : Bool(repository, "disabled") ? "disabled" : "active",
                createdAt = Text(repository, "created_at"),
                updatedAt = Text(repository, "updated_at"),
                archived = Bool(repository, "archived"),
                primaryLanguage = Text(repository, "language"),
                languages,
                sizeKb = Integer(repository, "size")
            })
        };

        var branches = await GetAll($"{root}/branches?per_page=100", cancellationToken);
        foreach (var branch in branches)
        {
            var branchName = Text(branch, "name") ?? string.Empty;
            var lastCommitId = Text(branch["commit"], "sha") ?? string.Empty;
            var commitDetail = string.IsNullOrEmpty(lastCommitId) ? new JObject() : await GetObject($"{root}/commits/{Uri.EscapeDataString(lastCommitId)}", cancellationToken);
            records.Add(Record("branch", branchName, repositoryId, new
            {
                name = branchName,
                isDefaultBranch = string.Equals(branchName, defaultBranch, StringComparison.Ordinal),
                isProtected = Bool(branch, "protected"),
                lastCommitId,
                lastActivityAt = Text(commitDetail["commit"]?["committer"], "date")
            }));
        }

        var commitBranches = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var branch in branches)
        {
            var branchName = Text(branch, "name");
            if (string.IsNullOrWhiteSpace(branchName)) continue;
            var commits = await GetAll($"{root}/commits?sha={Uri.EscapeDataString(branchName)}&per_page=100", cancellationToken);
            foreach (var summary in commits)
            {
                var sha = Text(summary, "sha") ?? string.Empty;
                if (sha.Length == 0) continue;
                if (!commitBranches.TryGetValue(sha, out var associatedBranches))
                    commitBranches[sha] = associatedBranches = new HashSet<string>(StringComparer.Ordinal);
                associatedBranches.Add(branchName);
            }
        }

        foreach (var (sha, associatedBranches) in commitBranches)
        {
                var commit = await GetObject($"{root}/commits/{Uri.EscapeDataString(sha)}", cancellationToken);
                var files = (commit["files"] as JArray ?? new JArray()).Select(file => new
                {
                    fileName = Text(file, "filename"),
                    status = Text(file, "status"),
                    additions = Integer(file, "additions"),
                    deletions = Integer(file, "deletions"),
                    changes = Integer(file, "changes")
                }).ToArray();
                records.Add(Record("commit", sha, repositoryId, new
                {
                    commitId = sha,
                    author = Text(commit["author"], "login") ?? Text(commit["commit"]?["author"], "name"),
                    committer = Text(commit["committer"], "login") ?? Text(commit["commit"]?["committer"], "name"),
                    commitDate = Text(commit["commit"]?["committer"], "date") ?? Text(commit["commit"]?["author"], "date"),
                    branch = associatedBranches.Contains(defaultBranch) ? defaultBranch : associatedBranches.Order(StringComparer.Ordinal).First(),
                    branches = associatedBranches.Order(StringComparer.Ordinal).ToArray(),
                    message = Text(commit["commit"], "message"),
                    filesChanged = files,
                    additions = Integer(commit["stats"], "additions"),
                    deletions = Integer(commit["stats"], "deletions")
                }));
        }

        var pullRequests = await GetAll($"{root}/pulls?state=all&sort=updated&direction=desc&per_page=100", cancellationToken);
        foreach (var pull in pullRequests)
        {
            var number = Text(pull, "number") ?? string.Empty;
            if (number.Length == 0) continue;
            var prRoot = $"{root}/pulls/{Uri.EscapeDataString(number)}";
            var reviews = await GetAll($"{prRoot}/reviews?per_page=100", cancellationToken);
            var requested = await GetObject($"{prRoot}/requested_reviewers", cancellationToken);
            var comments = await GetAll($"{root}/issues/{Uri.EscapeDataString(number)}/comments?per_page=100", cancellationToken);
            var reviewComments = await GetAll($"{prRoot}/comments?per_page=100", cancellationToken);
            var files = await GetAll($"{prRoot}/files?per_page=100", cancellationToken);
            var reviewers = reviews.Select(item => Text(item["user"], "login"))
                .Concat((requested["users"] as JArray ?? new JArray()).Select(item => Text(item, "login")))
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var latestStates = reviews.OrderBy(item => ParseDate(Text(item, "submitted_at")) ?? DateTimeOffset.MinValue)
                .GroupBy(item => Text(item["user"], "login") ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(group => Text(group.Last(), "state")?.ToLowerInvariant()).ToArray();
            var approval = latestStates.Contains("changes_requested") ? "changes_requested" : latestStates.Contains("approved") ? "approved" : "pending";
            var status = Bool(pull, "merged") ? "merged" : Text(pull, "state") ?? "unknown";
            records.Add(Record("pull_request", number, repositoryId, new
            {
                number = Integer(pull, "number"),
                title = Text(pull, "title"),
                author = Text(pull["user"], "login"),
                status,
                createdAt = Text(pull, "created_at"),
                updatedAt = Text(pull, "updated_at"),
                closedAt = Text(pull, "closed_at"),
                mergedAt = Text(pull, "merged_at"),
                sourceBranch = Text(pull["head"], "ref"),
                targetBranch = Text(pull["base"], "ref"),
                reviewers,
                reviewStatus = reviews.Count == 0 ? "unreviewed" : "reviewed",
                approvalStatus = approval,
                comments = comments.Count + reviewComments.Count,
                changes = files.Count,
                additions = Integer(pull, "additions"),
                deletions = Integer(pull, "deletions")
            }));
        }

        var releases = await GetAll($"{root}/releases?per_page=100", cancellationToken);
        foreach (var release in releases)
        {
            var id = Text(release, "id") ?? string.Empty;
            if (id.Length == 0) continue;
            var tag = Text(release, "tag_name");
            records.Add(Record("release", id, repositoryId, new
            {
                releaseId = LongInteger(release, "id"), name = Text(release, "name"), tag,
                version = tag, author = Text(release["author"], "login"),
                createdAt = Text(release, "created_at"), publishedAt = Text(release, "published_at"),
                draft = Bool(release, "draft"), prerelease = Bool(release, "prerelease")
            }));
        }

        var workflows = await GetAll($"{root}/actions/workflows?per_page=100", cancellationToken);
        foreach (var workflowResponse in workflows)
        {
            var definitions = workflowResponse["workflows"] as JArray ?? new JArray();
            foreach (var workflow in definitions)
            {
                var id = Text(workflow, "id") ?? string.Empty;
                if (id.Length == 0) continue;
                var workflowName = Text(workflow, "name") ?? string.Empty;
                records.Add(Record("workflow", id, repositoryId, new
                {
                    workflowId = LongInteger(workflow, "id"), name = workflowName, path = Text(workflow, "path"),
                    state = Text(workflow, "state"), url = Text(workflow, "html_url")
                }));
                var runs = await GetAll($"{root}/actions/workflows/{Uri.EscapeDataString(id)}/runs?per_page=100", cancellationToken);
                foreach (var runResponse in runs)
                foreach (var run in runResponse["workflow_runs"] as JArray ?? new JArray())
                {
                    var runId = Text(run, "id") ?? string.Empty;
                    if (runId.Length == 0) continue;
                    var started = ParseDate(Text(run, "run_started_at") ?? Text(run, "created_at"));
                    var completed = ParseDate(Text(run, "updated_at"));
                    string? failureReason = null;
                    if (Text(run, "conclusion") is "failure" or "timed_out")
                    {
                        var jobs = await GetAll($"{ApiRoot}/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/actions/runs/{Uri.EscapeDataString(runId)}/jobs?per_page=100", cancellationToken);
                        failureReason = string.Join("; ", jobs.SelectMany(jobResponse => jobResponse["jobs"] as JArray ?? new JArray())
                            .Where(job => Text(job, "conclusion") == "failure")
                            .Select(job => $"{Text(job, "name")}: {string.Join(", ", (job["steps"] as JArray ?? new JArray()).Where(step => Text(step, "conclusion") == "failure").Select(step => Text(step, "name")))}"));
                        if (string.IsNullOrWhiteSpace(failureReason)) failureReason = Text(run, "conclusion");
                    }
                    records.Add(Record("workflow_run", runId, id, new
                    {
                        runId = LongInteger(run, "id"), workflow = workflowName, branch = Text(run, "head_branch"),
                        trigger = Text(run, "event"), status = Text(run, "status"), result = Text(run, "conclusion"),
                        startedAt = started, completedAt = Text(run, "status") == "completed" ? completed : null,
                        durationSeconds = started.HasValue && completed.HasValue ? (long?)(completed.Value - started.Value).TotalSeconds : null,
                        failureReason
                    }));
                }
            }
        }

        await AddSecurityAlerts(root, repositoryId, records, "dependabot", cancellationToken);
        await AddSecurityAlerts(root, repositoryId, records, "code-scanning", cancellationToken);
        await AddSecurityAlerts(root, repositoryId, records, "secret-scanning", cancellationToken);
        return new GitHubSnapshotImportRequest { RepositoryUrl = canonicalUrl, Records = records };
    }

    private async Task AddSecurityAlerts(string root, string repositoryId, List<GitHubSnapshotRecord> records, string type, CancellationToken cancellationToken)
    {
        var (endpoint, states) = type switch
        {
            // GitHub's alert endpoints do not accept the generic `all` state.
            // Fetch each documented state so the snapshot includes resolved as
            // well as active alerts without relying on undocumented behavior.
            "dependabot" => ("dependabot/alerts", new[] { "open", "dismissed", "fixed", "auto_dismissed" }),
            "code-scanning" => ("code-scanning/alerts", new[] { "open", "closed", "fixed", "dismissed" }),
            _ => ("secret-scanning/alerts", new[] { "open", "resolved" })
        };

        var alertsById = new Dictionary<string, JObject>(StringComparer.Ordinal);
        foreach (var state in states)
        {
            var alerts = await GetAll($"{root}/{endpoint}?state={Uri.EscapeDataString(state)}&per_page=100", cancellationToken);
            foreach (var alert in alerts.OfType<JObject>())
            {
                var alertId = Text(alert, "number") ?? Text(alert, "id");
                if (!string.IsNullOrWhiteSpace(alertId)) alertsById[alertId] = alert;
            }
        }

        foreach (var (id, alert) in alertsById)
        {
            var secretLocations = type == "secret-scanning"
                ? await GetAll($"{root}/secret-scanning/alerts/{Uri.EscapeDataString(id)}/locations?per_page=100", cancellationToken)
                : new JArray();
            var severity = Text(alert["security_advisory"], "severity")
                ?? Text(alert["rule"], "severity")
                ?? Text(alert, "secret_type_display_name")
                ?? "unknown";
            var affected = type switch
            {
                "dependabot" => new JObject { ["package"] = Text(alert["dependency"]?["package"], "name"), ["manifest"] = Text(alert["dependency"], "manifest_path"), ["vulnerableVersionRange"] = Text(alert["security_vulnerability"], "vulnerable_version_range") },
                "code-scanning" => new JObject { ["rule"] = Text(alert["rule"], "description"), ["path"] = Text(alert["most_recent_instance"]?["location"], "path"), ["commitId"] = Text(alert["most_recent_instance"], "commit_sha") },
                _ => new JObject
                {
                    ["secretType"] = Text(alert, "secret_type_display_name"),
                    ["locations"] = new JArray(secretLocations.Select(location => new JObject
                    {
                        ["path"] = Text(location["details"], "path"),
                        ["lineNumber"] = Text(location["details"], "line_number"),
                        ["blobSha"] = Text(location, "blob_sha")
                    }))
                }
            };
            var state = Text(alert, "state") ?? Text(alert, "resolution") ?? "unknown";
            records.Add(Record("security_alert", $"{type}:{id}", repositoryId, new
            {
                alertId = $"{type}:{id}", alertType = type, repositoryUrl = root.Replace(ApiRoot + "/repos/", "https://github.com/", StringComparison.Ordinal),
                severity, affected, createdAt = Text(alert, "created_at"), updatedAt = Text(alert, "updated_at"),
                status = state, resolution = Text(alert, "resolution"), resolvedAt = Text(alert, "fixed_at") ?? Text(alert, "resolved_at")
            }));
        }
    }

    private async Task<JObject> GetObject(string url, CancellationToken cancellationToken)
    {
        var token = await GetJson(url, cancellationToken);
        return token as JObject ?? new JObject();
    }

    private async Task<JArray> GetAll(string url, CancellationToken cancellationToken)
    {
        var all = new JArray();
        var next = url;
        for (var page = 0; !string.IsNullOrEmpty(next); page++)
        {
            if (page >= MaxPages) throw new InvalidOperationException("GitHub pagination exceeded the 500 page safety limit.");
            using var response = await Send(next, cancellationToken);
            var token = JToken.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (token is JArray array) all.Merge(array);
            else if (token is JObject obj && (obj["workflows"] is JArray || obj["workflow_runs"] is JArray)) all.Add(obj);
            next = NextLink(response.Headers);
        }
        return all;
    }

    private async Task<JToken> GetJson(string url, CancellationToken cancellationToken)
    {
        using var response = await Send(url, cancellationToken);
        return JToken.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private async Task<HttpResponseMessage> Send(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("GitHub pagination returned an invalid API URL.");
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.UserAgent.ParseAdd("ProductHealthDashboard/1.0");
            request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode) return response;

            var rateLimited = response.StatusCode == (HttpStatusCode)429
                || (response.StatusCode == HttpStatusCode.Forbidden
                    && (response.Headers.RetryAfter is not null
                        || (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) && remaining.FirstOrDefault() == "0")));
            if (rateLimited && attempt < 2)
            {
                var delay = response.Headers.RetryAfter?.Delta;
                if (delay is null && response.Headers.RetryAfter?.Date is DateTimeOffset retryDate)
                    delay = retryDate - DateTimeOffset.UtcNow;
                if (delay is null && response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues)
                    && long.TryParse(resetValues.FirstOrDefault(), out var resetUnix))
                    delay = DateTimeOffset.FromUnixTimeSeconds(resetUnix) - DateTimeOffset.UtcNow;
                var wait = delay ?? TimeSpan.FromSeconds(2 * (attempt + 1));
                if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
                response.Dispose();
                if (wait > TimeSpan.FromSeconds(30)) throw new InvalidOperationException("GitHub rate limit requires a retry delay longer than 30 seconds; retry the sync later.");
                await Task.Delay(wait, cancellationToken);
                continue;
            }
            var status = response.StatusCode;
            var requestId = response.Headers.TryGetValues("X-GitHub-Request-Id", out var ids) ? ids.FirstOrDefault() : "unknown";
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (errorBody.Length > 600) errorBody = errorBody[..600];
            response.Dispose();
            var endpoint = uri.AbsolutePath + uri.Query;
            throw new InvalidOperationException(
                $"GitHub API request to '{endpoint}' failed with HTTP {(int)status} (request {requestId}). " +
                $"GitHub response: {errorBody}. Verify repository feature availability and token permissions; authorization tokens are not included in errors.");
        }
    }

    private static string? NextLink(HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues("Link", out var values)) return null;
        foreach (var part in values.SelectMany(value => value.Split(',')))
            if (part.Contains("rel=\"next\"", StringComparison.Ordinal))
            {
                var start = part.IndexOf('<'); var end = part.IndexOf('>');
                if (start >= 0 && end > start) return part[(start + 1)..end];
            }
        return null;
    }

    private static (string Owner, string Name, string Url) ParseRepositoryUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("GitHub repository URL must use https://github.com/owner/repository.");
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2) throw new ArgumentException("GitHub repository URL must use https://github.com/owner/repository.");
        var name = segments[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? segments[1][..^4] : segments[1];
        return (segments[0], name, $"https://github.com/{segments[0]}/{segments[1]}");
    }

    private static GitHubSnapshotRecord Record(string type, string id, string? parentId, object data) =>
        new() { EntityType = type, ExternalId = id, ParentExternalId = parentId, Data = JObject.FromObject(data) };
    private static string? Text(JToken? token, string property) => token?[property]?.Type is JTokenType.Null or JTokenType.Undefined ? null : token?[property]?.ToString();
    private static int? Integer(JToken? token, string property) => int.TryParse(Text(token, property), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    private static long? LongInteger(JToken? token, string property) => long.TryParse(Text(token, property), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    private static bool Bool(JToken? token, string property) => bool.TryParse(Text(token, property), out var value) && value;
    private static DateTimeOffset? ParseDate(string? value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) ? date.ToUniversalTime() : null;
}
