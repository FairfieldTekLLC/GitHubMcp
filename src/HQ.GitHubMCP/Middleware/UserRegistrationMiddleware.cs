using GitHubMCP.Services;

namespace GitHubMCP.Middleware;

/// <summary>
/// Ensures authenticated users are registered or updated in local storage per request.
/// </summary>
/// <param name="next">The next middleware delegate.</param>
public sealed class UserRegistrationMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Registers or updates the current authenticated user, then continues the pipeline.
    /// </summary>
    /// <param name="c">The current HTTP context.</param>
    /// <param name="users">The user registration service.</param>
    /// <returns>A task that completes when middleware processing finishes.</returns>
    public async Task InvokeAsync(HttpContext c, UserRegistrationService users)
    {
        if (c.User.Identity?.IsAuthenticated == true)
            await users.RegisterOrUpdateAsync(c.User, c.RequestAborted);

        await next(c);
    }
}
