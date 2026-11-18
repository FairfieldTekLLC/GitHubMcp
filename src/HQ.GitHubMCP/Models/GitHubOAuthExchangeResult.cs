namespace GitHubMCP.Models;

/// <summary>
/// Represents the result of exchanging a GitHub OAuth code for user tokens.
/// </summary>
public sealed record GitHubOAuthExchangeResult(
    long GitHubUserId,
    string Login,
    string AccessToken,
    DateTimeOffset? AccessTokenExpiresUtc,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiresUtc);
