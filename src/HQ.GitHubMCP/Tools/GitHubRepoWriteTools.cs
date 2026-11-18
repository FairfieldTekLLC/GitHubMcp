using System.ComponentModel;
using System.Text;
using GitHubMCP.Models;
using ModelContextProtocol.Server;

namespace GitHubMCP.Tools;

/// <summary>
/// Phase 2 MCP tools: file creation, modification, deletion, and branch
/// management. All tools require the Write permission on the target repository.
/// </summary>
/// <param name="exec">The shared tool executor.</param>
[McpServerToolType]
public sealed class GitHubRepoWriteTools(GitHubToolExecutor exec)
{
    /// <summary>
    /// Creates or updates a file on a branch.
    /// </summary>
    [McpServerTool]
    [Description("Creates or updates a file on a branch of an authorized repository. Omit fileSha to create a new file; pass the current file SHA (from ReadRepositoryFile) to update an existing one.")]
    public Task<object> WriteRepositoryFile(
        Guid repositoryId,
        string path,
        string content,
        string commitMessage,
        string? fileSha,
        string? branch,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(WriteRepositoryFile),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Put,
            r => $"repos/{r.Owner}/{r.Name}/contents/{path.TrimStart('/')}",
            new
            {
                message = commitMessage,
                content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                sha = string.IsNullOrWhiteSpace(fileSha) ? null : fileSha,
                branch
            },
            ct);

    /// <summary>
    /// Deletes a file on a branch.
    /// </summary>
    [McpServerTool]
    [Description("Deletes a file on a branch of an authorized repository. Requires the current file SHA (from ReadRepositoryFile).")]
    public Task<object> DeleteRepositoryFile(
        Guid repositoryId,
        string path,
        string commitMessage,
        string fileSha,
        string? branch,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(DeleteRepositoryFile),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Delete,
            r => $"repos/{r.Owner}/{r.Name}/contents/{path.TrimStart('/')}",
            new
            {
                message = commitMessage,
                sha = fileSha,
                branch
            },
            ct);

    /// <summary>
    /// Creates a new branch from an existing branch or commit SHA.
    /// </summary>
    [McpServerTool]
    [Description("Creates a new branch in an authorized repository from a source branch (defaults to the repository default branch).")]
    public async Task<object> CreateBranch(
        Guid repositoryId,
        string newBranchName,
        string? sourceBranch,
        CancellationToken ct)
    {
        // Resolve the source branch head SHA first.
        var head = await exec.ExecuteRepositoryApiAsync(
            nameof(CreateBranch) + ".ResolveHead",
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/git/ref/heads/{Uri.EscapeDataString(sourceBranch ?? r.DefaultBranch)}",
            null,
            ct);

        if (head is not System.Text.Json.JsonElement json ||
            !json.TryGetProperty("object", out var obj) ||
            !obj.TryGetProperty("sha", out var sha))
        {
            return new { Error = true, Message = "Unable to resolve source branch head SHA.", Detail = head };
        }

        return await exec.ExecuteRepositoryApiAsync(
            nameof(CreateBranch),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Post,
            r => $"repos/{r.Owner}/{r.Name}/git/refs",
            new
            {
                @ref = $"refs/heads/{newBranchName}",
                sha = sha.GetString()
            },
            ct);
    }

    /// <summary>
    /// Deletes a branch.
    /// </summary>
    [McpServerTool]
    [Description("Deletes a branch from an authorized repository. The repository default branch cannot be deleted.")]
    public Task<object> DeleteBranch(
        Guid repositoryId,
        string branchName,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(DeleteBranch),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Delete,
            r => branchName.Equals(r.DefaultBranch, StringComparison.OrdinalIgnoreCase)
                ? throw new InvalidOperationException("Deleting the default branch is not permitted.")
                : $"repos/{r.Owner}/{r.Name}/git/refs/heads/{Uri.EscapeDataString(branchName)}",
            null,
            ct);

    /// <summary>
    /// Shows the diff for a file between two refs.
    /// </summary>
    [McpServerTool]
    [Description("Shows the changes for a specific file between two branches or commits of an authorized repository.")]
    public async Task<object> GetFileDiff(
        Guid repositoryId,
        string baseRef,
        string headRef,
        string path,
        CancellationToken ct)
    {
        var compare = await exec.ExecuteRepositoryApiAsync(
            nameof(GetFileDiff),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/compare/{Uri.EscapeDataString(baseRef)}...{Uri.EscapeDataString(headRef)}",
            null,
            ct);

        if (compare is not System.Text.Json.JsonElement json ||
            !json.TryGetProperty("files", out var files))
        {
            return compare;
        }

        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("filename", out var name) &&
                string.Equals(name.GetString(), path, StringComparison.OrdinalIgnoreCase))
            {
                return file.Clone();
            }
        }

        return new { Error = true, Message = $"File '{path}' not changed between the specified refs." };
    }

    /// <summary>
    /// Retrieves a commit's changed files and patches for revert or cherry-pick.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves a commit's changed files and patches so the changes can be applied to another branch (cherry-pick) or inverted (revert) via WriteRepositoryFile. GitHub's REST API has no direct revert/cherry-pick endpoint.")]
    public Task<object> GetCommitPatch(
        Guid repositoryId,
        string commitSha,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetCommitPatch),
            repositoryId,
            RepositoryPermission.Write,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/commits/{commitSha}",
            null,
            ct);
}
