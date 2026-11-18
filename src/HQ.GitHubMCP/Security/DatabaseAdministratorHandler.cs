using System.Security.Claims;
using GitHubMCP.Data;
using GitHubMCP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GitHubMCP.Security;

public sealed class AdministratorRequirement
    : IAuthorizationRequirement
{
}

public sealed class DatabaseAdministratorHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseAdministratorHandler> logger)
    : AuthorizationHandler<AdministratorRequirement>
{
    private const string ExpectedTenantId =
        "2f06f5dd-fa90-43c2-a1d6-6a60b4b77a9e";

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdministratorRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        // Support both Entra and OpenIddict claim formats.
        var entraObjectId =
            context.User.FindFirstValue("oid") ??
            context.User.FindFirstValue(
                "http://schemas.microsoft.com/identity/claims/objectidentifier");

        var tenantId =
            context.User.FindFirstValue("tid") ??
            context.User.FindFirstValue(
                "http://schemas.microsoft.com/identity/claims/tenantid");

        if (string.IsNullOrWhiteSpace(entraObjectId) ||
            string.IsNullOrWhiteSpace(tenantId))
        {
            logger.LogWarning(
                "Administrator authorization denied: missing identity claims.");

            return;
        }

        if (!string.Equals(
                tenantId,
                ExpectedTenantId,
                StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Administrator authorization denied: unexpected tenant.");

            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var user = await db.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.EntraObjectId == entraObjectId);

            if (user is null)
            {
                logger.LogWarning(
                    "Administrator authorization denied: user not registered.");

                return;
            }

            if (!user.IsActive)
            {
                logger.LogWarning(
                    "Administrator authorization denied: inactive user.");

                return;
            }

            if (user.Role != UserRole.Administrator)
            {
                logger.LogWarning(
                    "Administrator authorization denied: insufficient role.");

                return;
            }

            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Database administrator authorization failed.");

            // Fail closed.
        }
    }
}