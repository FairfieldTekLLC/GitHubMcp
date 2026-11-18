using System.ComponentModel;
using GitHubMCP.Models;
using ModelContextProtocol.Server;

namespace GitHubMCP.Tools;

/// <summary>
/// Phase 4 MCP tools: GitHub Actions workflows, CI/CD, releases, and tags.
/// Reads require the Read permission; triggers and mutations require Write.
/// </summary>
/// <param name="exec">The shared tool executor.</param>
[McpServerToolType]
public sealed class GitHubActionsTools(GitHubToolExecutor exec)
{
    // ---------- Workflows ----------

    /// <summary>
    /// Lists GitHub Actions workflows.
    /// </summary>
    [McpServerTool]
    [Description("Lists GitHub Actions workflows in an authorized repository.")]
    public Task<object> ListWorkflows(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListWorkflows),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/actions/workflows",
            null,
            ct);

    /// <summary>
    /// Retrieves a workflow definition.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves a GitHub Actions workflow by ID or file name (e.g. build.yml).")]
    public Task<object> GetWorkflow(
        Guid repositoryId,
        string workflowIdOrFileName,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetWorkflow),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/actions/workflows/{Uri.EscapeDataString(workflowIdOrFileName)}",
            null,
            ct);

    /// <summary>
    /// Lists workflow runs.
    /// </summary>
    [McpServerTool]
    [Description("Lists workflow run history for an authorized repository, optionally filtered by workflow and branch.")]
    public Task<object> ListWorkflowRuns(
        Guid repositoryId,
        string? workflowIdOrFileName,
        string? branch,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListWorkflowRuns),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r =>
            {
                var basePath = string.IsNullOrWhiteSpace(workflowIdOrFileName)
                    ? $"repos/{r.Owner}/{r.Name}/actions/runs"
                    : $"repos/{r.Owner}/{r.Name}/actions/workflows/{Uri.EscapeDataString(workflowIdOrFileName)}/runs";
                var query = "per_page=30";

                if (!string.IsNullOrWhiteSpace(branch))
                {
                    query += $"&branch={Uri.EscapeDataString(branch)}";
                }

                return $"{basePath}?{query}";
            },
            null,
            ct);

    /// <summary>
    /// Retrieves a workflow run.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves the status and conclusion of a workflow run in an authorized repository.")]
    public Task<object> GetWorkflowRun(
        Guid repositoryId,
        long runId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetWorkflowRun),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/actions/runs/{runId}",
            null,
            ct);

    /// <summary>
    /// Lists the jobs and step results of a workflow run.
    /// </summary>
    [McpServerTool]
    [Description("Lists the jobs and step outcomes of a workflow run — useful for diagnosing build and deployment failures.")]
    public Task<object> GetWorkflowRunJobs(
        Guid repositoryId,
        long runId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetWorkflowRunJobs),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/actions/runs/{runId}/jobs?per_page=50",
            null,
            ct);

    /// <summary>
    /// Triggers a workflow dispatch event.
    /// </summary>
    [McpServerTool]
    [Description("Triggers a workflow (workflow_dispatch) on a branch of an authorized repository.")]
    public Task<object> TriggerWorkflow(
        Guid repositoryId,
        string workflowIdOrFileName,
        string? branch,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiWithBodyFactoryAsync(
            nameof(TriggerWorkflow),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/actions/workflows/{Uri.EscapeDataString(workflowIdOrFileName)}/dispatches",
            r => (object?)new { @ref = branch ?? r.DefaultBranch },
            ct);

    /// <summary>
    /// Reruns a workflow run.
    /// </summary>
    [McpServerTool]
    [Description("Reruns a workflow run (all jobs) in an authorized repository.")]
    public Task<object> RerunWorkflow(
        Guid repositoryId,
        long runId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(RerunWorkflow),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/actions/runs/{runId}/rerun",
            null,
            ct);

    /// <summary>
    /// Cancels a running workflow run.
    /// </summary>
    [McpServerTool]
    [Description("Cancels a running workflow run in an authorized repository.")]
    public Task<object> CancelWorkflow(
        Guid repositoryId,
        long runId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(CancelWorkflow),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/actions/runs/{runId}/cancel",
            null,
            ct);

    /// <summary>
    /// Lists artifacts produced by a workflow run.
    /// </summary>
    [McpServerTool]
    [Description("Lists build artifacts produced by a workflow run in an authorized repository.")]
    public Task<object> ListWorkflowArtifacts(
        Guid repositoryId,
        long runId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListWorkflowArtifacts),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/actions/runs/{runId}/artifacts",
            null,
            ct);

    /// <summary>
    /// Retrieves the combined CI status for a commit.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves the combined CI status and check runs for a commit in an authorized repository.")]
    public Task<object> GetCommitStatus(
        Guid repositoryId,
        string commitSha,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetCommitStatus),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/commits/{commitSha}/check-runs?per_page=50",
            null,
            ct);

    // ---------- Releases and tags ----------

    /// <summary>
    /// Lists releases or retrieves a single release by tag.
    /// </summary>
    [McpServerTool]
    [Description("Lists releases in an authorized repository, or retrieves a single release (including assets) when tagName is provided.")]
    public Task<object> ListReleases(
        Guid repositoryId,
        string? tagName,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListReleases),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => string.IsNullOrWhiteSpace(tagName)
                ? $"repos/{r.Owner}/{r.Name}/releases?per_page=30"
                : $"repos/{r.Owner}/{r.Name}/releases/tags/{Uri.EscapeDataString(tagName)}",
            null,
            ct);

    /// <summary>
    /// Creates a release.
    /// </summary>
    [McpServerTool]
    [Description("Creates a release for a tag in an authorized repository, optionally generating release notes automatically.")]
    public Task<object> CreateRelease(
        Guid repositoryId,
        string tagName,
        string? name,
        string? body,
        bool? draft,
        bool? prerelease,
        bool? generateReleaseNotes,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(CreateRelease),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/releases",
            new
            {
                tag_name = tagName,
                name,
                body,
                draft = draft ?? false,
                prerelease = prerelease ?? false,
                generate_release_notes = generateReleaseNotes ?? false
            },
            ct);

    /// <summary>
    /// Lists Git tags.
    /// </summary>
    [McpServerTool]
    [Description("Lists Git tags in an authorized repository.")]
    public Task<object> ListTags(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListTags),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/tags?per_page=100",
            null,
            ct);

    /// <summary>
    /// Generates release notes between two tags or refs.
    /// </summary>
    [McpServerTool]
    [Description("Generates release notes for a tag in an authorized repository, optionally relative to a previous tag.")]
    public Task<object> GenerateReleaseNotes(
        Guid repositoryId,
        string tagName,
        string? previousTagName,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GenerateReleaseNotes),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/releases/generate-notes",
            new { tag_name = tagName, previous_tag_name = previousTagName },
            ct);
}
