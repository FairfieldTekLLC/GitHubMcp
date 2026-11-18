using System.Security.Claims;
using GitHubMCP.Data;
using GitHubMCP.Models;
using Microsoft.EntityFrameworkCore;

namespace GitHubMCP.Services;

/// <summary>
/// Registers authenticated Entra users in local storage and keeps profile fields synchronized.
/// </summary>
/// <param name="db">The application data context.</param>
public sealed class UserRegistrationService(AppDbContext db)
{
    /// <summary>
    /// Registers a user if missing, or updates profile fields when already present.
    /// </summary>
    /// <param name="p">The authenticated user principal.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The persisted application user.</returns>
    public async Task<AppUser> RegisterOrUpdateAsync(ClaimsPrincipal p, CancellationToken ct)
    {
        var oid = p.FindFirstValue("oid")
                  ?? p.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                  ?? throw new UnauthorizedAccessException("Missing Entra object ID.");

        var tenantId = p.FindFirstValue("tid")
            ?? p.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");

        if (!string.Equals(
                tenantId,
                "2f06f5dd-fa90-43c2-a1d6-6a60b4b77a9e",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Invalid Entra tenant.");
        }

        var email = p.FindFirstValue(ClaimTypes.Email) ?? p.FindFirstValue("preferred_username") ?? string.Empty;
        var name = p.FindFirstValue("name")
            ?? p.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;

        var u = await db.Users
            .Include(x => x.GitHubIdentity)
            .SingleOrDefaultAsync(x => x.EntraObjectId == oid, ct);

        if (u is not null && !u.IsActive)
        {
            throw new UnauthorizedAccessException(
                "The application user account is inactive.");
        }

        if (u is null)
        {
            u = new AppUser
            {
                EntraObjectId = oid,
                Email = email,
                DisplayName = name,
                Role = UserRole.ReadOnly
            };
            db.Users.Add(u);
        }
        else
        {
            // Preserve existing profile information when MCP tokens
            // do not contain optional profile claims.

            if (!string.IsNullOrWhiteSpace(email))
            {
                u.Email = email;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                u.DisplayName = name;
            }
        }

        await db.SaveChangesAsync(ct);
        return u;
    }

    /// <summary>
    /// Resolves the current application user for the supplied principal.
    /// </summary>
    /// <param name="p">The authenticated user principal.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The current application user.</returns>
    public Task<AppUser> CurrentAsync(ClaimsPrincipal p, CancellationToken ct) => RegisterOrUpdateAsync(p, ct);
}
