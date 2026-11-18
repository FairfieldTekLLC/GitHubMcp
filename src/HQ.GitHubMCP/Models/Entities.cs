using System.ComponentModel.DataAnnotations;

namespace GitHubMCP.Models;

/// <summary>
/// Defines internal role levels for application users.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Read-only access.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// Developer-level access.
    /// </summary>
    Developer,

    /// <summary>
    /// Administrator-level access.
    /// </summary>
    Administrator
}

/// <summary>
/// Defines repository permission levels.
/// </summary>
public enum RepositoryPermission
{
    /// <summary>
    /// Read permission.
    /// </summary>
    Read=0,

    /// <summary>
    /// Write permission.
    /// </summary>
    Write=1,

    /// <summary>
    /// Administrative permission.
    /// </summary>
    Admin=2
}

/// <summary>
/// Defines approval-gated operation types.
/// </summary>
public enum ApprovalOperation
{
    /// <summary>
    /// File commit operation.
    /// </summary>
    Commit,

    /// <summary>
    /// Pull request operation.
    /// </summary>
    PullRequest
}

/// <summary>
/// Defines lifecycle states for approval requests.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// Waiting for user decision.
    /// </summary>
    Pending,

    /// <summary>
    /// Explicitly approved.
    /// </summary>
    Approved,

    /// <summary>
    /// Explicitly rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// Expired before execution.
    /// </summary>
    Expired,

    /// <summary>
    /// Successfully executed.
    /// </summary>
    Executed,

    /// <summary>
    /// Execution attempted and failed.
    /// </summary>
    Failed
}

/// <summary>
/// Represents an application user mapped from Entra identity.
/// </summary>
public sealed class AppUser
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the Entra object identifier.
    /// </summary>
    [MaxLength(100)]
    public string EntraObjectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user email address.
    /// </summary>
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the internal role.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.ReadOnly;

    /// <summary>
    /// Gets or sets whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the user was created.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the connected GitHub identity.
    /// </summary>
    public GitHubIdentity? GitHubIdentity { get; set; }
}

/// <summary>
/// Represents a connected GitHub identity for an application user.
/// </summary>
public sealed class GitHubIdentity
{
    /// <summary>
    /// Gets or sets the identity identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the related application user identifier.
    /// </summary>
    public Guid AppUserId { get; set; }

    /// <summary>
    /// Gets or sets the related application user.
    /// </summary>
    public AppUser AppUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the GitHub login name.
    /// </summary>
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub numeric user identifier.
    /// </summary>
    public long GitHubUserId { get; set; }

    /// <summary>
    /// Gets or sets the protected GitHub access token.
    /// </summary>
    [MaxLength(4000)]
    public string ProtectedAccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the access token expires in UTC.
    /// </summary>
    public DateTimeOffset? AccessTokenExpiresUtc { get; set; }

    /// <summary>
    /// Gets or sets the protected GitHub refresh token.
    /// </summary>
    [MaxLength(4000)]
    public string? ProtectedRefreshToken { get; set; }

    /// <summary>
    /// Gets or sets when the refresh token expires in UTC.
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresUtc { get; set; }

    /// <summary>
    /// Gets or sets when the identity was connected.
    /// </summary>
    public DateTimeOffset ConnectedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a GitHub repository registered in the application.
/// </summary>
public sealed class GitHubRepository
{
    /// <summary>
    /// Gets or sets the repository identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the repository owner or organization.
    /// </summary>
    [MaxLength(100)]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default branch.
    /// </summary>
    [MaxLength(100)]
    public string DefaultBranch { get; set; } = "main";

    /// <summary>
    /// Gets or sets whether the repository is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets when the repository was registered.
    /// </summary>
    public DateTimeOffset RegisteredUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents granted repository access for a user.
/// </summary>
public sealed class RepositoryGrant
{
    /// <summary>
    /// Gets or sets the grant identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid AppUserId { get; set; }

    /// <summary>
    /// Gets or sets the related user.
    /// </summary>
    public AppUser AppUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the repository identifier.
    /// </summary>
    public Guid GitHubRepositoryId { get; set; }

    /// <summary>
    /// Gets or sets the related repository.
    /// </summary>
    public GitHubRepository Repository { get; set; } = null!;

    /// <summary>
    /// Gets or sets the granted permission level.
    /// </summary>
    public RepositoryPermission Permission { get; set; } = RepositoryPermission.Read;
}

/// <summary>
/// Represents a pending or completed approval request for a repository operation.
/// </summary>
public sealed class ApprovalRequest
{
    /// <summary>
    /// Gets or sets the approval request identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the requesting user identifier.
    /// </summary>
    public Guid RequestedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the requesting user.
    /// </summary>
    public AppUser RequestedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the target repository identifier.
    /// </summary>
    public Guid RepositoryId { get; set; }

    /// <summary>
    /// Gets or sets the target repository.
    /// </summary>
    public GitHubRepository Repository { get; set; } = null!;

    /// <summary>
    /// Gets or sets the requested operation.
    /// </summary>
    public ApprovalOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the request status.
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// Gets or sets the source branch.
    /// </summary>
    [MaxLength(150)]
    public string SourceBranch { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target branch.
    /// </summary>
    [MaxLength(150)]
    public string TargetBranch { get; set; } = "main";

    /// <summary>
    /// Gets or sets the request title.
    /// </summary>
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized operation payload.
    /// </summary>
    public string PayloadJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the integrity hash of the payload JSON.
    /// </summary>
    [MaxLength(64)]
    public string PayloadSha256 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integrity hash of the single-use approval token.
    /// </summary>
    [MaxLength(64)]
    public string SingleUseNonceHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the request was created.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets when the request expires.
    /// </summary>
    public DateTimeOffset ExpiresUtc { get; set; }

    /// <summary>
    /// Gets or sets the deciding user identifier.
    /// </summary>
    public Guid? DecidedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the deciding user.
    /// </summary>
    public AppUser? DecidedBy { get; set; }

    /// <summary>
    /// Gets or sets when a decision was recorded.
    /// </summary>
    public DateTimeOffset? DecidedUtc { get; set; }

    /// <summary>
    /// Gets or sets when execution completed.
    /// </summary>
    public DateTimeOffset? ExecutedUtc { get; set; }

    /// <summary>
    /// Gets or sets the resulting URL from execution, when available.
    /// </summary>
    [MaxLength(500)]
    public string? ResultUrl { get; set; }

    /// <summary>
    /// Gets or sets the failure reason when execution fails.
    /// </summary>
    [MaxLength(2000)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the row version for optimistic concurrency.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// Represents an audit log record.
/// </summary>
public sealed class AuditLog
{
    /// <summary>
    /// Gets or sets the audit log identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the acting user identifier.
    /// </summary>
    public Guid? AppUserId { get; set; }

    /// <summary>
    /// Gets or sets the action name.
    /// </summary>
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    [MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outcome.
    /// </summary>
    [MaxLength(64)]
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets serialized detail payload.
    /// </summary>
    public string DetailsJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets when the audit event was created.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
