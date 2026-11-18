using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GitHubMCP.Data;
using GitHubMCP.Models;
using Microsoft.EntityFrameworkCore;

namespace GitHubMCP.Services;

/// <summary>
/// Manages the approval lifecycle for commit and pull request operations.
/// </summary>
/// <param name="db">The application data context.</param>
/// <param name="cfg">Application configuration.</param>
/// <param name="access">Repository access validation service.</param>
/// <param name="gh">GitHub API service.</param>
/// <param name="audit">Audit logging service.</param>
public sealed class ApprovalService(
    AppDbContext db,
    IConfiguration cfg,
    RepositoryAccessService access,
    GitHubService gh,
    AuditService audit)
{
    /// <summary>
    /// Hashes a string using SHA-256 and returns an uppercase hexadecimal value.
    /// </summary>
    /// <param name="s">The input string.</param>
    /// <returns>The SHA-256 hash in hexadecimal form.</returns>
    private static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

    /// <summary>
    /// Canonicalizes a payload object as camelCase JSON for integrity hashing.
    /// </summary>
    /// <param name="p">The payload object.</param>
    /// <returns>The canonical JSON representation.</returns>
    private static string Canon(object p) =>
        JsonSerializer.Serialize(p, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    /// <summary>
    /// Creates a new pending approval request and returns its single-use approval token.
    /// </summary>
    /// <param name="u">The requesting user.</param>
    /// <param name="r">The target repository.</param>
    /// <param name="op">The requested operation type.</param>
    /// <param name="payload">The operation payload to execute after approval.</param>
    /// <param name="source">The source branch.</param>
    /// <param name="target">The target branch.</param>
    /// <param name="title">The request title.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The persisted request and plaintext approval token.</returns>
    public async Task<(ApprovalRequest Request, string ApprovalToken)> CreateAsync(
        AppUser u,
        GitHubRepository r,
        ApprovalOperation op,
        object payload,
        string source,
        string target,
        string title,
        CancellationToken ct)
    {
        var persistedUser = await db.Users
            .Include(x => x.GitHubIdentity)
            .SingleOrDefaultAsync(
                x => x.Id == u.Id &&
                     x.EntraObjectId == u.EntraObjectId,
                ct)
            ?? throw new UnauthorizedAccessException(
                "The requesting user is not registered.");

        if (!persistedUser.IsActive)
        {
            throw new UnauthorizedAccessException(
                "The requesting user is inactive.");
        }

        var persistedRepository = await db.Repositories
            .SingleOrDefaultAsync(
                x => x.Id == r.Id && x.IsEnabled,
                ct)
            ?? throw new InvalidOperationException(
                "The repository is not registered or enabled.");

        await access.EnsureAsync(
            persistedUser,
            persistedRepository,
            RepositoryPermission.Write,
            ct);

        var json = Canon(payload);
        var token = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        var req = new ApprovalRequest
        {
            RequestedByUserId = persistedUser.Id,
            RequestedBy = persistedUser,
            RepositoryId = persistedRepository.Id,
            Repository = persistedRepository,
            Operation = op,
            Status = ApprovalStatus.Pending,
            PayloadJson = json,
            PayloadSha256 = Hash(json),
            SingleUseNonceHash = Hash(token),
            SourceBranch = source,
            TargetBranch = target,
            Title = title,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(
                cfg.GetValue("Approval:LifetimeMinutes", 15))
        };

        db.ApprovalRequests.Add(req);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            persistedUser.Id,
            "ApprovalRequested",
            "ApprovalRequest",
            req.Id.ToString(),
            "Pending",
            new
            {
                op,
                persistedRepository.Owner,
                persistedRepository.Name
            },
            ct);

        return (req, token);
    }

    /// <summary>
    /// Records an approval decision for a pending request.
    /// </summary>
    /// <param name="id">The approval request identifier.</param>
    /// <param name="actor">The acting user.</param>
    /// <param name="approve">Whether to approve the request.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>A task that completes when the decision is persisted.</returns>
    public async Task DecideAsync(Guid id, AppUser actor, bool approve, CancellationToken ct)
    {
        var r = await db.ApprovalRequests.Include(x => x.Repository).SingleAsync(x => x.Id == id, ct);

        if (actor.Role != UserRole.Administrator ||
            !actor.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Administrator permission is required.");
        }

        if (r.Status != ApprovalStatus.Pending || r.ExpiresUtc <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Request is not pending or has expired.");

        r.Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        r.DecidedByUserId = actor.Id;
        r.DecidedUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(actor.Id, approve ? "ApprovalApproved" : "ApprovalRejected", "ApprovalRequest", id.ToString(), r.Status.ToString(), null, ct);
    }

    /// <summary>
    /// Executes an approved request by invoking the corresponding GitHub operation.
    /// </summary>
    /// <param name="id">The approval request identifier.</param>
    /// <param name="actor">The acting user.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>A task that completes when execution and persistence are finished.</returns>
    public async Task ExecuteAsync(Guid id, AppUser actor, CancellationToken ct)
    {
        var r = await db.ApprovalRequests
            .Include(x => x.Repository)
            .Include(x => x.RequestedBy)
            .ThenInclude(x => x.GitHubIdentity)
            .SingleAsync(x => x.Id == id, ct);

        if (r.RequestedByUserId != actor.Id || r.Status != ApprovalStatus.Approved)
            throw new InvalidOperationException("A valid explicit approval is required.");

        if (r.ExpiresUtc <= DateTimeOffset.UtcNow)
        {
            r.Status = ApprovalStatus.Expired;
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Approval expired.");
        }

        if (Hash(r.PayloadJson) != r.PayloadSha256)
            throw new InvalidOperationException("Payload changed after approval.");

        await access.EnsureAsync(actor, r.Repository, RepositoryPermission.Write, ct);

        try
        {
            r.ResultUrl = r.Operation == ApprovalOperation.Commit
                ? await gh.CreateCommitAsync(actor, r.Repository, JsonSerializer.Deserialize<CommitPayload>(r.PayloadJson)!, ct)
                : await gh.CreatePullRequestAsync(actor, r.Repository, JsonSerializer.Deserialize<PullRequestPayload>(r.PayloadJson)!, ct);

            r.Status = ApprovalStatus.Executed;
            r.ExecutedUtc = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            r.Status = ApprovalStatus.Failed;
            r.FailureReason = ex.Message;
            throw;
        }
        finally
        {
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(actor.Id, "ApprovalExecuted", "ApprovalRequest", id.ToString(), r.Status.ToString(), new { r.ResultUrl, r.FailureReason }, ct);
        }
    }
}
