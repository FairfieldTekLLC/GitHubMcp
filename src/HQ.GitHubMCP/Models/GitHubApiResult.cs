namespace GitHubMCP.Models;

/// <summary>
/// Represents the outcome of a GitHub API call made on behalf of a user.
/// </summary>
/// <param name="StatusCode">The HTTP status code.</param>
/// <param name="IsSuccess">Whether the call succeeded.</param>
/// <param name="Body">The raw JSON response body.</param>
public sealed record GitHubApiResult(
    int StatusCode,
    bool IsSuccess,
    string Body);
