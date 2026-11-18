using System.Text.Json.Serialization;

namespace GitHubMCP.Models;

/// <summary>
/// OAuth Dynamic Client Registration (RFC 7591) request payload.
/// </summary>
public sealed class DynamicClientRegistrationRequest
{
    /// <summary>
    /// Gets the client display name.
    /// </summary>
    [JsonPropertyName("client_name")]
    public string? ClientName { get; init; }

    /// <summary>
    /// Gets the requested redirect URIs.
    /// </summary>
    [JsonPropertyName("redirect_uris")]
    public string[]? RedirectUris { get; init; }

    /// <summary>
    /// Gets the requested grant types.
    /// </summary>
    [JsonPropertyName("grant_types")]
    public string[]? GrantTypes { get; init; }

    /// <summary>
    /// Gets the requested response types.
    /// </summary>
    [JsonPropertyName("response_types")]
    public string[]? ResponseTypes { get; init; }

    /// <summary>
    /// Gets the requested token endpoint authentication method.
    /// </summary>
    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; init; }

    /// <summary>
    /// Gets the requested scopes as a space-delimited string.
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}
