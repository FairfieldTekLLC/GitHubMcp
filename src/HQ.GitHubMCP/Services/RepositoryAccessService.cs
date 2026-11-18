using GitHubMCP.Data;
using GitHubMCP.Models;
using Microsoft.EntityFrameworkCore;

namespace GitHubMCP.Services;

/// <summary>
/// Validates whether a user can perform operations against a registered repository.
/// </summary>
/// <param name="db">The application data context.</param>
/// <param name="gh">The GitHub API service.</param>
public sealed class RepositoryAccessService(AppDbContext db, GitHubService gh)
{
    /// <summary>
    /// Ensures that a user has both internal grant permission and matching GitHub-side permission.
    /// </summary>
    /// <param name="u">The acting application user.</param>
    /// <param name="r">The target repository.</param>
    /// <param name="needed">The required permission level.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>A task that completes when authorization checks pass.</returns>
    public async Task EnsureAsync(AppUser u, GitHubRepository r, RepositoryPermission needed, CancellationToken ct)
    {
        if (!u.IsActive)
            throw new UnauthorizedAccessException("Active user registration required.");

        var grant = await db.RepositoryGrants.SingleOrDefaultAsync(
            x => x.AppUserId == u.Id && x.GitHubRepositoryId == r.Id,
            ct)
            ?? throw new UnauthorizedAccessException("No HQ.GitHubMCP repository grant.");

        if (grant.Permission < needed)
            throw new UnauthorizedAccessException("Insufficient internal repository permission.");

        if (gh.IsGitHubAppMode)
        {
            if (!await gh.HasInstallationPermissionAsync(r, needed, ct))
                throw new UnauthorizedAccessException(
                    "GitHub App installation lacks repository permission.");
        }
        else
        {
            if (u.GitHubIdentity is null)
                throw new UnauthorizedAccessException(
                    "GitHub account registration required.");

            await gh.EnsureUserAccessTokenAsync(u, ct);

            if (!await gh.HasGitHubPermissionAsync(u, r, needed, ct))
                throw new UnauthorizedAccessException(
                    "GitHub user permission check failed.");
        }
    }
}
