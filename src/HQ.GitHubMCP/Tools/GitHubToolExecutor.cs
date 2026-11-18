using System.Text.Json;
using GitHubMCP.Data;
using GitHubMCP.Models;
using GitHubMCP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GitHubMCP.Tools;

/// <summary>
/// Shared plumbing for repository-scoped MCP tools: resolves the current user,
/// resolves and authorizes the target repository, executes GitHub API calls,
/// and converts responses to structured results.
/// </summary>
/// <param name="db">The application data context.</param>
/// <param name="users">The user registration service.</param>
/// <param name="access">The repository access validation service.</param>
/// <param name="gh">The GitHub service.</param>
/// <param name="accessor">The HTTP context accessor.</param>
/// <param name="logger">The logger.</param>
public sealed class GitHubToolExecutor(
    AppDbContext db,
    UserRegistrationService users,
    RepositoryAccessService access,
    GitHubService gh,
    IHttpContextAccessor accessor,
    ILogger<GitHubToolExecutor> logger)
{
    /// <summary>
    /// Gets the application data context.
    /// </summary>
    public AppDbContext Db => db;

    /// <summary>
    /// Gets the GitHub service.
    /// </summary>
    public GitHubService GitHub => gh;

    /// <summary>
    /// Resolves the currently authenticated application user.
    /// </summary>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The current application user.</returns>
    public Task<AppUser> CurrentUserAsync(CancellationToken ct)
        => users.CurrentAsync(accessor.HttpContext!.User, ct);

    /// <summary>
    /// Resolves an enabled repository and validates the user's permission.
    /// </summary>
    /// <param name="user">The acting user.</param>
    /// <param name="repositoryId">The repository identifier.</param>
    /// <param name="permission">The required permission level.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The authorized repository.</returns>
    public async Task<GitHubRepository> AuthorizeAsync(
        AppUser user,
        Guid repositoryId,
        RepositoryPermission permission,
        CancellationToken ct)
    {
        var repository = await db.Repositories
            .SingleOrDefaultAsync(
                x => x.Id == repositoryId && x.IsEnabled,
                ct);

        if (repository is null)
        {
            throw new InvalidOperationException(
                "Repository not found or not enabled.");
        }

        await access.EnsureAsync(user, repository, permission, ct);

        return repository;
    }

    /// <summary>
    /// Executes an authorized GitHub API call against a repository and returns
    /// the parsed JSON response.
    /// </summary>
    /// <param name="toolName">The invoking tool name used for logging.</param>
    /// <param name="repositoryId">The repository identifier.</param>
    /// <param name="permission">The required permission level.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="pathFactory">Builds the API path from the repository.</param>
    /// <param name="body">An optional JSON request body.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The parsed JSON response.</returns>
    public Task<object> ExecuteRepositoryApiAsync(
        string toolName,
        Guid repositoryId,
        RepositoryPermission permission,
        HttpMethod method,
        Func<GitHubRepository, string> pathFactory,
        object? body,
        CancellationToken ct)
        => ExecuteRepositoryApiWithBodyFactoryAsync(
            toolName,
            repositoryId,
            permission,
            method,
            pathFactory,
            _ => body,
            ct);

    /// <summary>
    /// Executes an authorized GitHub API call against a repository, building
    /// both the path and body from the resolved repository, and returns the
    /// parsed JSON response.
    /// </summary>
    /// <param name="toolName">The invoking tool name used for logging.</param>
    /// <param name="repositoryId">The repository identifier.</param>
    /// <param name="permission">The required permission level.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="pathFactory">Builds the API path from the repository.</param>
    /// <param name="bodyFactory">Builds the request body from the repository.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The parsed JSON response.</returns>
    public async Task<object> ExecuteRepositoryApiWithBodyFactoryAsync(
        string toolName,
        Guid repositoryId,
        RepositoryPermission permission,
        HttpMethod method,
        Func<GitHubRepository, string> pathFactory,
        Func<GitHubRepository, object?> bodyFactory,
        CancellationToken ct)
    {
        var invocationId = Guid.NewGuid().ToString("N");

        logger.LogInformation(
            "GitHubToolExecutor.{Tool} started. InvocationId={InvocationId}, RepositoryId={RepositoryId}",
            toolName,
            invocationId,
            repositoryId);

        var user = await CurrentUserAsync(ct);
        var repository = await AuthorizeAsync(user, repositoryId, permission, ct);
        var path = pathFactory(repository);
        var body = bodyFactory(repository);
        var result = await gh.SendApiAsync(user, method, path, body, ct);

        logger.LogInformation(
            "GitHubToolExecutor.{Tool} completed. InvocationId={InvocationId}, HTTP={Status}",
            toolName,
            invocationId,
            result.StatusCode);

        return ToResult(result);
    }

    /// <summary>
    /// Converts a GitHub API result into a structured tool response, failing
    /// closed on non-success status codes.
    /// </summary>
    /// <param name="result">The API result.</param>
    /// <returns>The parsed JSON payload or an error object.</returns>
    public static object ToResult(GitHubApiResult result)
    {
        if (!result.IsSuccess)
        {
            return new
            {
                Error = true,
                result.StatusCode,
                Message = Truncate(result.Body, 2000)
            };
        }

        if (string.IsNullOrWhiteSpace(result.Body))
        {
            return new { Success = true, result.StatusCode };
        }

        using var doc = JsonDocument.Parse(result.Body);

        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Truncates a string to the specified maximum length.
    /// </summary>
    /// <param name="value">The value to truncate.</param>
    /// <param name="max">The maximum length.</param>
    /// <returns>The truncated string.</returns>
    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
