using System.ComponentModel.DataAnnotations;

namespace GitHubMCP.Models;

/// <summary>
/// Represents a dynamically registered OAuth client.
/// </summary>
public sealed class OAuthClientRegistration
{
    /// <summary>
    /// Gets or sets the registration identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the OAuth client identifier.
    /// </summary>
    [MaxLength(200)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the protected client secret, if confidential.
    /// </summary>
    [MaxLength(4000)]
    public string? ProtectedClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the client display name.
    /// </summary>
    [MaxLength(200)]
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the software statement identifier.
    /// </summary>
    [MaxLength(200)]
    public string? SoftwareId { get; set; }

    /// <summary>
    /// Gets or sets the software version.
    /// </summary>
    [MaxLength(100)]
    public string? SoftwareVersion { get; set; }

    /// <summary>
    /// Gets or sets allowed redirect URIs as JSON.
    /// </summary>
    [MaxLength(8000)]
    public string RedirectUrisJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets allowed grant types as JSON.
    /// </summary>
    [MaxLength(2000)]
    public string GrantTypesJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets allowed scopes as JSON.
    /// </summary>
    [MaxLength(2000)]
    public string ScopesJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets token endpoint authentication method.
    /// </summary>
    [MaxLength(100)]
    public string TokenEndpointAuthMethod { get; set; } = "none";

    /// <summary>
    /// Gets or sets whether PKCE S256 is required.
    /// </summary>
    public bool RequirePkceS256 { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the client is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the client was created.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets when the client was last updated.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
