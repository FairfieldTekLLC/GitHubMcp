using System.ComponentModel;
using GitHubMCP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace GitHubMCP.Tools;

/// <summary>
/// Phase 5 MCP tools: security alert inspection (read-only, Admin permission)
/// and enterprise governance/self-service tools (SQL-grant based, no repository
/// permission required beyond authentication).
/// </summary>
/// <param name="exec">The shared tool executor.</param>
/// <param name="logger">The logger.</param>
[McpServerToolType]
public sealed class GitHubSecurityGovernanceTools(
    GitHubToolExecutor exec,
    ILogger<GitHubSecurityGovernanceTools> logger)
{
    // ---------- Security (read-only, Admin) ----------

    /// <summary>
    /// Lists or retrieves security alerts of a given type.
    /// </summary>
    [McpServerTool]
    [Description("Lists security alerts for an authorized repository (requires Admin grant). AlertType must be dependabot, code-scanning, or secret-scanning. Provide alertNumber to retrieve a single dependabot or code-scanning alert.")]
    public Task<object> ListSecurityAlerts(
        Guid repositoryId,
        string alertType,
        string? state,
        int? alertNumber,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListSecurityAlerts),
            repositoryId,
            RepositoryPermission.Admin,
            HttpMethod.Get,
            r =>
            {
                var segment = alertType.ToLowerInvariant() switch
                {
                    "dependabot" => "dependabot/alerts",
                    "code-scanning" => "code-scanning/alerts",
                    "secret-scanning" => "secret-scanning/alerts",
                    _ => throw new InvalidOperationException(
                        "alertType must be dependabot, code-scanning, or secret-scanning.")
                };

                return alertNumber is not null
                    ? $"repos/{r.Owner}/{r.Name}/{segment}/{alertNumber}"
                    : $"repos/{r.Owner}/{r.Name}/{segment}?state={Uri.EscapeDataString(state ?? "open")}&per_page=50";
            },
            null,
            ct);

    /// <summary>
    /// Inspects branch protection on a branch.
    /// </summary>
    [McpServerTool]
    [Description("Retrieves branch protection rules for a branch of an authorized repository (requires Admin grant).")]
    public Task<object> GetBranchProtection(
        Guid repositoryId,
        string? branch,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetBranchProtection),
            repositoryId,
            RepositoryPermission.Admin,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/branches/{Uri.EscapeDataString(branch ?? r.DefaultBranch)}/protection",
            null,
            ct);

    /// <summary>
    /// Lists repository collaborators.
    /// </summary>
    [McpServerTool]
    [Description("Lists collaborators and their roles for an authorized repository (requires Admin grant).")]
    public Task<object> ListRepositoryCollaborators(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ListRepositoryCollaborators),
            repositoryId,
            RepositoryPermission.Admin,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/collaborators?per_page=100",
            null,
            ct);

    // ---------- Governance (self-service) ----------

    /// <summary>
    /// Shows the current user's SQL repository grants.
    /// </summary>
    [McpServerTool]
    [Description("Shows the current user's HQ.GitHubMCP repository grants and permission levels.")]
    public async Task<object> ListMyRepositoryGrants(CancellationToken ct)
    {
        var u = await exec.CurrentUserAsync(ct);

        var grants = await exec.Db.RepositoryGrants
            .Where(g => g.AppUserId == u.Id)
            .Select(g => new
            {
                RepositoryId = g.Repository.Id,
                g.Repository.Owner,
                g.Repository.Name,
                g.Repository.IsEnabled,
                Permission = g.Permission.ToString()
            })
            .ToListAsync(ct);

        logger.LogInformation(
            "ListMyRepositoryGrants: UserId={UserId}, Count={Count}",
            u.Id,
            grants.Count);

        return grants;
    }

    /// <summary>
    /// Shows the current user's connected GitHub identity.
    /// </summary>
    [McpServerTool]
    [Description("Shows the current user's connected GitHub identity and token freshness (no secrets returned).")]
    public async Task<object> GetMyGitHubIdentity(CancellationToken ct)
    {
        var u = await exec.CurrentUserAsync(ct);

        var identity = await exec.Db.GitHubIdentities
            .Where(i => i.AppUserId == u.Id)
            .Select(i => new
            {
                i.Login,
                i.GitHubUserId,
                i.ConnectedUtc,
                i.AccessTokenExpiresUtc,
                HasRefreshToken = i.ProtectedRefreshToken != null,
                i.RefreshTokenExpiresUtc
            })
            .SingleOrDefaultAsync(ct);

        return identity is null
            ? new { Connected = false, Message = "No GitHub identity connected. Visit the profile page to connect." }
            : (object)new { Connected = true, Identity = identity };
    }

    /// <summary>
    /// Validates effective access to a repository without performing changes.
    /// </summary>
    [McpServerTool]
    [Description("Validates connectivity and effective access to an authorized repository by calling the GitHub API read-only.")]
    public Task<object> ValidateRepositoryConnection(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(ValidateRepositoryConnection),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}",
            null,
            ct);

    /// <summary>
    /// Checks the health of the MCP service.
    /// </summary>
    [McpServerTool]
    [Description("Checks MCP service health: authentication, database connectivity, and GitHub App mode.")]
    public async Task<object> GetMcpHealth(CancellationToken ct)
    {
        var u = await exec.CurrentUserAsync(ct);
        var canQueryDb = await exec.Db.Repositories.AnyAsync(ct);

        return new
        {
            Healthy = true,
            Authenticated = true,
            UserActive = u.IsActive,
            DatabaseReachable = canQueryDb || true,
            GitHubAppMode = exec.GitHub.IsGitHubAppMode,
            TimestampUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Retrieves recent repository activity (events).
    /// </summary>
    [McpServerTool]
    [Description("Retrieves recent activity events for an authorized repository.")]
    public Task<object> GetRepositoryActivity(
        Guid repositoryId,
        CancellationToken ct)
        => exec.ExecuteRepositoryApiAsync(
            nameof(GetRepositoryActivity),
            repositoryId,
            RepositoryPermission.Read,
            HttpMethod.Get,
            r => $"repos/{r.Owner}/{r.Name}/events?per_page=30",
            null,
            ct);
}
