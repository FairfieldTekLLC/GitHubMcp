using System.ComponentModel.DataAnnotations;

namespace GitHubMCP.Models;

/// <summary>
/// Dynamic client registration request payload.
/// </summary>
public sealed class OAuthDynamicClientRegistrationRequest
{
    /// <summary>
    /// Gets or sets the display name for the client.
    /// </summary>
    [MaxLength(200)]
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets redirect URIs.
    /// </summary>
    public string[] RedirectUris { get; set; } = [];

    /// <summary>
    /// Gets or sets allowed grant types.
    /// </summary>
    public string[] GrantTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets requested scopes.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets requested token endpoint auth method.
    /// </summary>
    public string? TokenEndpointAuthMethod { get; set; }

    /// <summary>
    /// Gets or sets optional software identifier.
    /// </summary>
    [MaxLength(200)]
    public string? SoftwareId { get; set; }

    /// <summary>
    /// Gets or sets optional software version.
    /// </summary>
    [MaxLength(100)]
    public string? SoftwareVersion { get; set; }
}

/// <summary>
/// Dynamic client registration response payload.
/// </summary>
public sealed class OAuthDynamicClientRegistrationResponse
{
    public string ClientId { get; set; } = string.Empty;

    public string? ClientSecret { get; set; }

    public long ClientIdIssuedAt { get; set; }

    public long? ClientSecretExpiresAt { get; set; }

    public string[] RedirectUris { get; set; } = [];

    public string[] GrantTypes { get; set; } = [];

    public string Scope { get; set; } = "MCP.Access";

    public string TokenEndpointAuthMethod { get; set; } = "none";

    public string? ClientName { get; set; }

    public string? SoftwareId { get; set; }

    public string? SoftwareVersion { get; set; }
}
