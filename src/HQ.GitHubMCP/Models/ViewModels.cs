using System.ComponentModel.DataAnnotations;

namespace GitHubMCP.Models;

/// <summary>
/// Represents repository registration form input.
/// </summary>
public sealed class RepositoryViewModel
{
    /// <summary>
    /// Gets or sets the repository owner or organization.
    /// </summary>
    [Required]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default branch name.
    /// </summary>
    [Required]
    public string DefaultBranch { get; set; } = "main";
}

/// <summary>
/// Represents repository grant assignment input.
/// </summary>
public sealed class GrantViewModel
{
    /// <summary>
    /// Gets or sets the target application user identifier.
    /// </summary>
    public Guid AppUserId { get; set; }

    /// <summary>
    /// Gets or sets the target repository identifier.
    /// </summary>
    public Guid RepositoryId { get; set; }

    /// <summary>
    /// Gets or sets the granted repository permission level.
    /// </summary>
    public RepositoryPermission Permission { get; set; }
}

/// <summary>
/// Represents one file mutation proposal for commit execution.
/// </summary>
/// <param name="Path">The repository-relative file path.</param>
/// <param name="Content">The desired file content.</param>
/// <param name="ExpectedSha">Optional expected current blob SHA for optimistic concurrency.</param>
public sealed record ProposedFile(string Path, string Content, string? ExpectedSha = null);

/// <summary>
/// Represents an approval payload for commit execution.
/// </summary>
/// <param name="Branch">The target branch.</param>
/// <param name="Message">The commit message.</param>
/// <param name="Files">The proposed file changes.</param>
public sealed record CommitPayload(string Branch, string Message, IReadOnlyList<ProposedFile> Files);

/// <summary>
/// Represents an approval payload for pull request creation.
/// </summary>
/// <param name="Head">The source branch.</param>
/// <param name="Base">The target branch.</param>
/// <param name="Title">The pull request title.</param>
/// <param name="Body">The pull request body.</param>
public sealed record PullRequestPayload(string Head, string Base, string Title, string Body);
