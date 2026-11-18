using System.ComponentModel;
using GitHubMCP.Data;
using GitHubMCP.Models;
using GitHubMCP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace GitHubMCP.Tools;

/// <summary>
/// MCP tools for listing repositories and performing direct or approval-gated GitHub operations.
/// </summary>
/// <param name="db">The application data context.</param>
/// <param name="users">The user registration service.</param>
/// <param name="approvals">The approval request service.</param>
/// <param name="access">The repository access validation service.</param>
/// <param name="gh">The GitHub service.</param>
/// <param name="audit">The audit logging service.</param>
/// <param name="accessor">The HTTP context accessor.</param>
/// <param name="logger">The logger.</param>
[McpServerToolType]
public sealed class GitHubTools(
    AppDbContext db,
    UserRegistrationService users,
    ApprovalService approvals,
    RepositoryAccessService access,
    GitHubService gh,
    AuditService audit,
    IHttpContextAccessor accessor,
    ILogger<GitHubTools> logger)
{
    /// <summary>
    /// Lists repositories currently authorized for the signed-in user.
    /// </summary>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>A list of repositories and effective permissions.</returns>
    [McpServerTool]
    [Description("Lists repositories authorized by HQ.GitHubMCP for the current user.")]
    public async Task<object> ListRepositories(CancellationToken ct)
    {
        var invocationId = Guid.NewGuid().ToString("N");

        logger.LogInformation(
            "GitHubTools.ListRepositories started. InvocationId={InvocationId}",
            invocationId);

        var claims = accessor.HttpContext?
            .User.Claims
            .Select(c => c.Type)
            .Distinct()
            .ToArray();

        logger.LogInformation(
            "MCP authenticated claim types: {Claims}. InvocationId={InvocationId}",
            string.Join(", ", claims ?? []),
            invocationId);

        var u = await users.CurrentAsync(accessor.HttpContext!.User, ct);

        var repositories = await db.RepositoryGrants
            .Where(g => g.AppUserId == u.Id && g.Repository.IsEnabled)
            .Select(g => new
            {
                g.Repository.Id,
                g.Repository.Owner,
                g.Repository.Name,
                g.Repository.DefaultBranch,
                Permission = g.Permission.ToString()
            })
            .ToListAsync(ct);

        logger.LogInformation(
            "GitHubTools.ListRepositories completed. InvocationId={InvocationId}, Count={Count}",
            invocationId,
            repositories.Count);

        return repositories;
    }

    /// <summary>
    /// Commits files directly to an authorized GitHub repository.
    /// </summary>
    /// <param name="repositoryId">The target repository identifier.</param>
    /// <param name="branch">The target branch for the commit.</param>
    /// <param name="message">The commit message.</param>
    /// <param name="files">The files to include in the commit.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The resulting GitHub URL.</returns>
    [McpServerTool]
    [Description("Commits files directly to an authorized GitHub repository.")]
    public async Task<string> CommitFiles(
        Guid repositoryId,
        string branch,
        string message,
        List<ProposedFile> files,
        CancellationToken ct)
    {
        var invocationId = Guid.NewGuid().ToString("N");

        logger.LogInformation(
            "GitHubTools.CommitFiles started. InvocationId={InvocationId}, RepositoryId={RepositoryId}, Branch={Branch}, FileCount={FileCount}",
            invocationId,
            repositoryId,
            branch,
            files.Count);

        var user = await users.CurrentAsync(
            accessor.HttpContext!.User,
            ct);

        var repository = await db.Repositories
            .SingleOrDefaultAsync(
                x => x.Id == repositoryId && x.IsEnabled,
                ct);

        if (repository is null)
        {
            throw new InvalidOperationException(
                "Repository not found or disabled.");
        }

        await access.EnsureAsync(
            user,
            repository,
            RepositoryPermission.Write,
            ct);

        var payload = new CommitPayload(
            branch,
            message,
            files);

        var resultUrl = await gh.CreateCommitAsync(
            user,
            repository,
            payload,
            ct);

        await audit.WriteAsync(
            user.Id,
            "DirectCommit",
            "Repository",
            repository.Id.ToString(),
            "Success",
            new
            {
                repository.Owner,
                repository.Name,
                branch,
                message
            },
            ct);

        logger.LogInformation(
            "GitHubTools.CommitFiles completed. InvocationId={InvocationId}, RepositoryId={RepositoryId}, Url={Url}",
            invocationId,
            repository.Id,
            resultUrl);

        return resultUrl;
    }

    /// <summary>
    /// Creates a pull request directly in an authorized GitHub repository.
    /// </summary>
    /// <param name="repositoryId">The target repository identifier.</param>
    /// <param name="head">The source branch.</param>
    /// <param name="base">The target branch.</param>
    /// <param name="title">The pull request title.</param>
    /// <param name="body">The pull request body.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The resulting pull request URL.</returns>
    [McpServerTool]
    [Description("Creates a pull request directly in an authorized GitHub repository.")]
    public async Task<string> CreatePullRequest(
        Guid repositoryId,
        string head,
        string @base,
        string title,
        string body,
        CancellationToken ct)
    {
        var invocationId = Guid.NewGuid().ToString("N");

        logger.LogInformation(
            "GitHubTools.CreatePullRequest started. InvocationId={InvocationId}, RepositoryId={RepositoryId}, Head={Head}, Base={Base}",
            invocationId,
            repositoryId,
            head,
            @base);

        var user = await users.CurrentAsync(
            accessor.HttpContext!.User,
            ct);

        var repository = await db.Repositories
            .SingleOrDefaultAsync(
                x => x.Id == repositoryId && x.IsEnabled,
                ct);

        if (repository is null)
        {
            throw new InvalidOperationException(
                "Repository not found or disabled.");
        }

        await access.EnsureAsync(
            user,
            repository,
            RepositoryPermission.Write,
            ct);

        var payload = new PullRequestPayload(
            head,
            @base,
            title,
            body);

        var resultUrl = await gh.CreatePullRequestAsync(
            user,
            repository,
            payload,
            ct);

        await audit.WriteAsync(
            user.Id,
            "DirectPullRequest",
            "Repository",
            repository.Id.ToString(),
            "Success",
            new
            {
                repository.Owner,
                repository.Name,
                head,
                @base,
                title
            },
            ct);

        logger.LogInformation(
            "GitHubTools.CreatePullRequest completed. InvocationId={InvocationId}, RepositoryId={RepositoryId}, Url={Url}",
            invocationId,
            repository.Id,
            resultUrl);

        return resultUrl;
    }

    /// <summary>
    /// Verifies both internal SQL repository grants and the
    /// connected user's effective GitHub permissions.
    /// This tool never modifies a repository.
    /// </summary>
    [McpServerTool]
    [Description(
        "Checks whether the current user has effective Read, Write, " +
        "or Admin permission on a repository. Validates both SQL RBAC " +
        "and GitHub permissions without modifying anything.")]
    public async Task<object> CheckRepositoryPermission(
        Guid repositoryId,
        string permission,
        CancellationToken ct)
    {
        var invocationId = Guid.NewGuid().ToString("N");

        logger.LogInformation(
            "GitHubTools.CheckRepositoryPermission started. InvocationId={InvocationId}, RepositoryId={RepositoryId}, Permission={Permission}",
            invocationId,
            repositoryId,
            permission);

        var principal = accessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException(
                "Authenticated MCP context required.");

        var user = await users.CurrentAsync(principal, ct);

        var repository = await db.Repositories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == repositoryId && x.IsEnabled,
                ct);

        if (repository is null)
        {
            return new
            {
                Authorized = false,
                Reason = "Repository not found or disabled."
            };
        }

        if (!Enum.TryParse<RepositoryPermission>(
                permission,
                ignoreCase: true,
                out var needed) ||
            !Enum.IsDefined(needed))
        {
            return new
            {
                Authorized = false,
                Reason = "Invalid permission. Use Read, Write, or Admin."
            };
        }

        try
        {
            await access.EnsureAsync(
                user,
                repository,
                needed,
                ct);

            logger.LogInformation(
                "GitHubTools.CheckRepositoryPermission completed. InvocationId={InvocationId}, RepositoryId={RepositoryId}, Authorized=true",
                invocationId,
                repository.Id);

            return new
            {
                Authorized = true,
                Repository = $"{repository.Owner}/{repository.Name}",
                RepositoryId = repository.Id,
                RequiredPermission = needed.ToString(),
                SqlGrantVerified = true,
                GitHubPermissionVerified = true,
                Message = "Both authorization checks succeeded."
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(
                "Repository permission check denied for {RepositoryId}: {Reason}. InvocationId={InvocationId}",
                repositoryId,
                ex.Message,
                invocationId);

            logger.LogInformation(
                "GitHubTools.CheckRepositoryPermission completed. InvocationId={InvocationId}, RepositoryId={RepositoryId}, Authorized=false",
                invocationId,
                repositoryId);

            return new
            {
                Authorized = false,
                Repository = $"{repository.Owner}/{repository.Name}",
                RepositoryId = repository.Id,
                RequiredPermission = needed.ToString(),
                Reason = ex.Message
            };
        }
    }

    /// <summary>
    /// Creates a pending commit approval request without executing any repository changes.
    /// </summary>
    /// <param name="repositoryId">The target repository identifier.</param>
    /// <param name="branch">The target branch for the commit.</param>
    /// <param name="message">The commit message.</param>
    /// <param name="files">The files to include in the commit.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>Approval request details including review URL and expiration.</returns>
    [McpServerTool]
    [Description("Creates a pending commit approval request. This never commits code. The user must approve in the trusted portal and then execute it.")]
    public async Task<object> RequestCommitApproval(Guid repositoryId, string branch, string message, List<ProposedFile> files, CancellationToken ct)
    {
        var invocationId = Guid.NewGuid().ToString("N");

        logger.LogInformation(
            "GitHubTools.RequestCommitApproval started. InvocationId={InvocationId}, RepositoryId={RepositoryId}, Branch={Branch}, FileCount={FileCount}",
            invocationId,
            repositoryId,
            branch,
            files.Count);

        var u = await users.CurrentAsync(accessor.HttpContext!.User, ct);
        var r = await db.Repositories.SingleAsync(x => x.Id == repositoryId, ct);
        var x = await approvals.CreateAsync(
            u,
            r,
            ApprovalOperation.Commit,
            new CommitPayload(branch, message, files),
            branch,
            r.DefaultBranch,
            message,
            ct);

        logger.LogInformation(
            "GitHubTools.RequestCommitApproval completed. InvocationId={InvocationId}, RepositoryId={RepositoryId}, RequestId={RequestId}",
            invocationId,
            repositoryId,
            x.Request.Id);

        return new
        {
            x.Request.Id,
            ReviewUrl = $"/Approvals/Review/{x.Request.Id}",
            x.Request.ExpiresUtc,
            Message = "No commit has been created. Explicit administrator approval is required."
        };
    }

    /// <summary>
    /// Creates a pending pull request approval request without creating the pull request.
    /// </summary>
    /// <param name="repositoryId">The target repository identifier.</param>
    /// <param name="head">The source branch.</param>
    /// <param name="base">The target branch.</param>
    /// <param name="title">The pull request title.</param>
    /// <param name="body">The pull request body.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>Approval request details including review URL and expiration.</returns>
    [McpServerTool]
    [Description("Creates a pending pull request approval request. This never creates a PR. The user must approve in the trusted portal and then execute it.")]
    public async Task<object> RequestPullRequestApproval(Guid repositoryId, string head, string @base, string title, string body, CancellationToken ct)
    {
        var invocationId = Guid.NewGuid().ToString("N");

        logger.LogInformation(
            "GitHubTools.RequestPullRequestApproval started. InvocationId={InvocationId}, RepositoryId={RepositoryId}, Head={Head}, Base={Base}",
            invocationId,
            repositoryId,
            head,
            @base);

        var u = await users.CurrentAsync(accessor.HttpContext!.User, ct);
        var r = await db.Repositories.SingleAsync(x => x.Id == repositoryId, ct);
        var x = await approvals.CreateAsync(
            u,
            r,
            ApprovalOperation.PullRequest,
            new PullRequestPayload(head, @base, title, body),
            head,
            @base,
            title,
            ct);

        logger.LogInformation(
            "GitHubTools.RequestPullRequestApproval completed. InvocationId={InvocationId}, RepositoryId={RepositoryId}, RequestId={RequestId}",
            invocationId,
            repositoryId,
            x.Request.Id);

        return new
        {
            x.Request.Id,
            ReviewUrl = $"/Approvals/Review/{x.Request.Id}",
            x.Request.ExpiresUtc,
            Message = "No pull request has been created. Explicit administrator approval is required."
        };
    }
}
