using System.ComponentModel;
using GitHubMCP.Models;
using ModelContextProtocol.Server;

namespace GitHubMCP.Tools;

/// <summary>
/// Phase 3 MCP tools: pull request and issue management.
/// Reads require the Read permission; mutations require the Write permission.
/// </summary>
/// <param name="exec">The shared tool executor.</param>
[McpServerToolType]
public sealed class GitHubPullRequestIssueTools(GitHubToolExecutor exec)
{
    // ---------- Pull requests ----------

    /// <summary>
    /// Lists pull requests.
    /// </summary>
    [McpServerTool]
    [Description("Lists pull requests in an authorized repository. State can be open, closed, or all.")]
    public Task<object> ListPullRequests(
        Guid repositoryId,
        string? state,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListPullRequests),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/pulls?state={Uri.EscapeDataString(state ?? "open")}&per_page=50",
            null,
            ct);

    /// <summary>
    /// Retrieves a pull request, its changed files, or its reviews.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves pull request information in an authorized repository. Set include to 'details' (default), 'files' (changed files with patches), or 'reviews' (review history).")]
    public Task<object> GetPullRequestDetails(
        Guid repositoryId,
        int pullNumber,
        string? include,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetPullRequestDetails),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => (include?.ToLowerInvariant()) switch
            {
                "files" => $"repos/{r.Owner}/{r.Name}/pulls/{pullNumber}/files?per_page=100",
                "reviews" => $"repos/{r.Owner}/{r.Name}/pulls/{pullNumber}/reviews",
                _ => $"repos/{r.Owner}/{r.Name}/pulls/{pullNumber}"
            },
            null,
            ct);

    /// <summary>
    /// Updates the title or description of a pull request.
    /// </summary>
    [McpServerTool]
    [Description("Updates the title and/or body of a pull request in an authorized repository.")]
    public Task<object> UpdatePullRequest(
        Guid repositoryId,
        int pullNumber,
        string? title,
        string? body,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(UpdatePullRequest),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Patch,
            r => $"repos/{r.Owner}/{r.Name}/pulls/{pullNumber}",
            new { title, body },
            ct);

    /// <summary>
    /// Adds a comment to a pull request.
    /// </summary>
    [McpServerTool]
    [Description("Adds a comment to a pull request in an authorized repository.")]
    public Task<object> AddPullRequestComment(
        Guid repositoryId,
        int pullNumber,
        string comment,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(AddPullRequestComment),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/issues/{pullNumber}/comments",
            new { body = comment },
            ct);

    /// <summary>
    /// Submits a review on a pull request.
    /// </summary>
    [McpServerTool]
    [Description("Submits a pull request review. Event must be APPROVE, REQUEST_CHANGES, or COMMENT.")]
    public Task<object> ReviewPullRequest(
        Guid repositoryId,
        int pullNumber,
        string reviewEvent,
        string? body,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ReviewPullRequest),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/pulls/{pullNumber}/reviews",
            new { @event = reviewEvent, body },
            ct);

    /// <summary>
    /// Merges an approved pull request.
    /// </summary>
    [McpServerTool]
    [Description("Merges a pull request in an authorized repository. Merge method can be merge, squash, or rebase.")]
    public Task<object> MergePullRequest(
        Guid repositoryId,
        int pullNumber,
        string? mergeMethod,
        string? commitMessage,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(MergePullRequest),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Put,
            r => $"repos/{r.Owner}/{r.Name}/pulls/{pullNumber}/merge",
            new { merge_method = mergeMethod ?? "squash", commit_message = commitMessage },
            ct);

    /// <summary>
    /// Closes a pull request without merging.
    /// </summary>
    [McpServerTool]
    [Description("Closes a pull request without merging in an authorized repository.")]
    public Task<object> ClosePullRequest(
        Guid repositoryId,
        int pullNumber,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ClosePullRequest),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Patch,
            r => $"repos/{r.Owner}/{r.Name}/pulls/{pullNumber}",
            new { state = "closed" },
            ct);

    // ---------- Issues ----------

    /// <summary>
    /// Lists issues or retrieves a single issue.
    /// </summary>
    [McpServerTool]
    [Description("Lists issues in an authorized repository, or retrieves a single issue when issueNumber is provided. State can be open, closed, or all; optional comma-separated labels filter.")]
    public Task<object> ListIssues(
        Guid repositoryId,
        int? issueNumber,
        string? state,
        string? labels,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListIssues),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r =>
            {
                if (issueNumber is not null)
                {
                    return $"repos/{r.Owner}/{r.Name}/issues/{issueNumber}";
                }

                var query = $"state={Uri.EscapeDataString(state ?? "open")}&per_page=50";

                if (!string.IsNullOrWhiteSpace(labels))
                {
                    query += $"&labels={Uri.EscapeDataString(labels)}";
                }

                return $"repos/{r.Owner}/{r.Name}/issues?{query}";
            },
            null,
            ct);

    /// <summary>
    /// Creates an issue.
    /// </summary>
    [McpServerTool]
    [Description("Creates an issue in an authorized repository with optional comma-separated labels and assignees.")]
    public Task<object> CreateIssue(
        Guid repositoryId,
        string title,
        string? body,
        string? labels,
        string? assignees,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(CreateIssue),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/issues",
            new
            {
                title,
                body,
                labels = Split(labels),
                assignees = Split(assignees)
            },
            ct);

    /// <summary>
    /// Updates an issue.
    /// </summary>
    [McpServerTool]
    [Description("Updates an issue's title, body, or state (open/closed) in an authorized repository.")]
    public Task<object> UpdateIssue(
        Guid repositoryId,
        int issueNumber,
        string? title,
        string? body,
        string? state,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(UpdateIssue),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Patch,
            r => $"repos/{r.Owner}/{r.Name}/issues/{issueNumber}",
            new { title, body, state },
            ct);

    /// <summary>
    /// Adds a comment to an issue.
    /// </summary>
    [McpServerTool]
    [Description("Adds a comment to an issue in an authorized repository.")]
    public Task<object> AddIssueComment(
        Guid repositoryId,
        int issueNumber,
        string comment,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(AddIssueComment),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/issues/{issueNumber}/comments",
            new { body = comment },
            ct);

    /// <summary>
    /// Assigns users to an issue.
    /// </summary>
    [McpServerTool]
    [Description("Assigns one or more users (comma-separated logins) to an issue in an authorized repository.")]
    public Task<object> AssignIssue(
        Guid repositoryId,
        int issueNumber,
        string assignees,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(AssignIssue),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/issues/{issueNumber}/assignees",
            new { assignees = Split(assignees) },
            ct);

    /// <summary>
    /// Lists repository labels.
    /// </summary>
    [McpServerTool]
    [Description("Lists issue labels defined in an authorized repository.")]
    public Task<object> ListLabels(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListLabels),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/labels?per_page=100",
            null,
            ct);

    /// <summary>
    /// Applies labels to an issue.
    /// </summary>
    [McpServerTool]
    [Description("Applies comma-separated labels to an issue in an authorized repository.")]
    public Task<object> AddIssueLabels(
        Guid repositoryId,
        int issueNumber,
        string labels,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(AddIssueLabels),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/issues/{issueNumber}/labels",
            new { labels = Split(labels) },
            ct);

    /// <summary>
    /// Lists milestones.
    /// </summary>
    [McpServerTool]
    [Description("Lists milestones in an authorized repository.")]
    public Task<object> ListMilestones(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListMilestones),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/milestones?state=all&per_page=100",
            null,
            ct);

    /// <summary>
    /// Splits a comma-separated list into an array.
    /// </summary>
    /// <param name="value">The comma-separated value.</param>
    /// <returns>The array, or an empty array when null or blank.</returns>
    private static string[] Split(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
