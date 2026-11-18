using System.ComponentModel;
using GitHubMCP.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace GitHubMCP.Tools;

/// <summary>
/// Phase 1 MCP tools: repository browsing, reading, and code analysis.
/// All tools require the Read permission on the target repository.
/// </summary>
/// <param name="exec">The shared tool executor.</param>
[McpServerToolType]
public sealed class GitHubRepoBrowseTools(GitHubToolExecutor exec)
{
    /// <summary>
    /// Lists files and folders at a path within a repository.
    /// </summary>
    [McpServerTool]
    [Description("Lists files and folders at a path within an authorized repository. Pass an empty path for the repository root.")]
    public Task<object> ListRepositoryContents(
        Guid repositoryId,
        string path,
        string? branch,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListRepositoryContents),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/contents/{path?.TrimStart('/')}{RefQuery(r, branch)}",
            null,
            ct);

    /// <summary>
    /// Reads the contents of a file within a repository.
    /// </summary>
    [McpServerTool]
    [Description("Reads a file from an authorized repository. Returns content (base64-encoded by GitHub), SHA, size, and path.")]
    public Task<object> ReadRepositoryFile(
        Guid repositoryId,
        string path,
        string? branch,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ReadRepositoryFile),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/contents/{path.TrimStart('/')}{RefQuery(r, branch)}",
            null,
            ct);

    /// <summary>
    /// Retrieves the complete recursive directory tree of a repository branch.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves the complete recursive file tree for a branch of an authorized repository.")]
    public Task<object> GetRepositoryTree(
        Guid repositoryId,
        string? branch,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetRepositoryTree),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/git/trees/{branch ?? r.DefaultBranch}?recursive=1",
            null,
            ct);

    /// <summary>
    /// Searches source code within a repository.
    /// </summary>
    [McpServerTool]
    [Description("Searches source code within an authorized repository using GitHub code search syntax.")]
    public Task<object> SearchRepositoryCode(
        Guid repositoryId,
        string query,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(SearchRepositoryCode),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"search/code?q={Uri.EscapeDataString($"{query} repo:{r.Owner}/{r.Name}")}",
            null,
            ct);

    /// <summary>
    /// Lists branches in a repository.
    /// </summary>
    [McpServerTool]
    [Description("Lists branches in an authorized repository.")]
    public Task<object> ListRepositoryBranches(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListRepositoryBranches),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/branches?per_page=100",
            null,
            ct);

    /// <summary>
    /// Retrieves repository metadata and settings.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves repository metadata and settings for an authorized repository.")]
    public Task<object> GetRepositoryInfo(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetRepositoryInfo),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}",
            null,
            ct);

    /// <summary>
    /// Identifies programming languages used in a repository.
    /// </summary>
    [McpServerTool]
    [Description("Identifies programming languages and byte counts in an authorized repository.")]
    public Task<object> GetRepositoryLanguages(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetRepositoryLanguages),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/languages",
            null,
            ct);

    /// <summary>
    /// Retrieves the repository README.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves the README of an authorized repository (content is base64-encoded by GitHub).")]
    public Task<object> GetRepositoryReadme(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetRepositoryReadme),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/readme",
            null,
            ct);

    /// <summary>
    /// Lists repository contributors.
    /// </summary>
    [McpServerTool]
    [Description("Lists contributors to an authorized repository.")]
    public Task<object> GetRepositoryContributors(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetRepositoryContributors),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/contributors?per_page=100",
            null,
            ct);

    /// <summary>
    /// Retrieves commit history for a branch or path.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves commit history for an authorized repository, optionally filtered by branch and file path.")]
    public Task<object> GetCommitHistory(
        Guid repositoryId,
        string? branch,
        string? path,
        int? perPage,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetCommitHistory),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r =>
            {
                var query = $"sha={Uri.EscapeDataString(branch ?? r.DefaultBranch)}&per_page={Math.Clamp(perPage ?? 30, 1, 100)}";

                if (!string.IsNullOrWhiteSpace(path))
                {
                    query += $"&path={Uri.EscapeDataString(path)}";
                }

                return $"repos/{r.Owner}/{r.Name}/commits?{query}";
            },
            null,
            ct);

    /// <summary>
    /// Retrieves the details of a single commit, including changed files.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves details of a single commit (message, author, stats, changed files with patches).")]
    public Task<object> GetCommitDetails(
        Guid repositoryId,
        string commitSha,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetCommitDetails),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/commits/{commitSha}",
            null,
            ct);

    /// <summary>
    /// Compares two branches, tags, or commits.
    /// </summary>
    [McpServerTool]
    [Description("Compares two branches, tags, or commit SHAs and returns the diff summary and changed files.")]
    public Task<object> CompareBranches(
        Guid repositoryId,
        string baseRef,
        string headRef,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(CompareBranches),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/compare/{Uri.EscapeDataString(baseRef)}...{Uri.EscapeDataString(headRef)}",
            null,
            ct);

    /// <summary>
    /// Builds a ref query string for content endpoints.
    /// </summary>
    /// <param name="r">The repository.</param>
    /// <param name="branch">The optional branch.</param>
    /// <returns>The query string.</returns>
    private static string RefQuery(GitHubRepository r, string? branch)
        => $"?ref={Uri.EscapeDataString(branch ?? r.DefaultBranch)}";
}
