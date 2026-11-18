using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GitHubMCP.Data;
using GitHubMCP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace GitHubMCP.Controllers;

/// <summary>
/// Hosts OAuth 2.1 dynamic client registration and authorization server endpoints.
/// </summary>
[ApiController]
public sealed class OAuthController(
    AppDbContext db,
    IConfiguration cfg,
    IOpenIddictApplicationManager appManager)
    : Controller
{
    private static readonly string[] AllowedGrantTypes =
    [
        GrantTypes.AuthorizationCode,
        GrantTypes.RefreshToken
    ];

    private static readonly string[] AllowedTokenEndpointAuthMethods =
    [
        ClientAuthenticationMethods.None,
        ClientAuthenticationMethods.ClientSecretPost,
        ClientAuthenticationMethods.ClientSecretBasic
    ];

    /// <summary>
    /// Returns OAuth authorization server metadata.
    /// </summary>
    [NonAction]
    public IActionResult AuthorizationServerMetadata()
    {
        var issuer = (cfg["OAuth:Issuer"] ??
                      "https://hq-githubmcp.bfsaul.net/")
            .TrimEnd('/') + "/";

        var registrationEndpoint = $"{issuer}oauth/register";
        var authorizationEndpoint = $"{issuer}oauth/authorize";
        var tokenEndpoint = $"{issuer}oauth/token";

        return new JsonResult(new
        {
            issuer,
            authorization_endpoint = authorizationEndpoint,
            token_endpoint = tokenEndpoint,
            registration_endpoint = registrationEndpoint,
            response_types_supported = new[] { "code" },
            response_modes_supported = new[] { "query" },
            grant_types_supported = AllowedGrantTypes,
            token_endpoint_auth_methods_supported =
                AllowedTokenEndpointAuthMethods,
            code_challenge_methods_supported = new[] { "S256" },
            scopes_supported = new[] { "MCP.Access", Scopes.OfflineAccess },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" }
        });
    }

    /// <summary>
    /// Dynamically registers an OAuth client.
    /// </summary>
    [NonAction]
    public async Task<IActionResult> RegisterClient(
        [FromBody] OAuthDynamicClientRegistrationRequest request,
        CancellationToken ct)
    {
        if (request.RedirectUris.Length == 0)
        {
            return BadRequest(new
            {
                error = "invalid_redirect_uri",
                error_description = "At least one redirect_uri is required."
            });
        }

        foreach (var uri in request.RedirectUris)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ||
                parsed.Scheme != Uri.UriSchemeHttps)
            {
                return BadRequest(new
                {
                    error = "invalid_redirect_uri",
                    error_description = "All redirect_uris must be absolute HTTPS URLs."
                });
            }
        }

        var grantTypes = request.GrantTypes.Length == 0
            ? new[] { GrantTypes.AuthorizationCode, GrantTypes.RefreshToken }
            : request.GrantTypes;

        if (grantTypes.Any(x => !AllowedGrantTypes.Contains(x, StringComparer.Ordinal)))
        {
            return BadRequest(new
            {
                error = "invalid_client_metadata",
                error_description = "Only authorization_code and refresh_token grant types are supported."
            });
        }

        var tokenMethod = string.IsNullOrWhiteSpace(request.TokenEndpointAuthMethod)
            ? ClientAuthenticationMethods.None
            : request.TokenEndpointAuthMethod;

        if (!AllowedTokenEndpointAuthMethods.Contains(tokenMethod, StringComparer.Ordinal))
        {
            return BadRequest(new
            {
                error = "invalid_client_metadata",
                error_description = "Unsupported token_endpoint_auth_method."
            });
        }

        var scopes = string.IsNullOrWhiteSpace(request.Scope)
            ? new[] { "MCP.Access" }
            : request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (scopes.Any(x => !string.Equals(x, "MCP.Access", StringComparison.Ordinal) &&
                            !string.Equals(x, Scopes.OfflineAccess, StringComparison.Ordinal)))
        {
            return BadRequest(new
            {
                error = Errors.InvalidScope,
                error_description = "Only MCP.Access and offline_access scopes are supported."
            });
        }

        var clientId = $"mcp_{Guid.NewGuid():N}";
        string? clientSecret = null;
        string? protectedSecret = null;

        if (!string.Equals(tokenMethod, ClientAuthenticationMethods.None, StringComparison.Ordinal))
        {
            clientSecret = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Guid.NewGuid().ToString("N");
            protectedSecret = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(clientSecret)));
        }

        var registration = new OAuthClientRegistration
        {
            ClientId = clientId,
            ProtectedClientSecret = protectedSecret,
            ClientName = request.ClientName,
            SoftwareId = request.SoftwareId,
            SoftwareVersion = request.SoftwareVersion,
            RedirectUrisJson = JsonSerializer.Serialize(request.RedirectUris),
            GrantTypesJson = JsonSerializer.Serialize(grantTypes),
            ScopesJson = JsonSerializer.Serialize(scopes),
            TokenEndpointAuthMethod = tokenMethod,
            RequirePkceS256 = true,
            IsActive = true,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        db.OAuthClientRegistrations.Add(registration);

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ConsentType = ConsentTypes.Explicit,
            DisplayName = request.ClientName ?? "MCP Dynamic Client",
            ClientType = string.Equals(tokenMethod, ClientAuthenticationMethods.None, StringComparison.Ordinal)
                ? ClientTypes.Public
                : ClientTypes.Confidential
        };

        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);

        if (grantTypes.Contains(GrantTypes.RefreshToken, StringComparer.Ordinal))
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        }

        descriptor.Permissions.Add(Permissions.Prefixes.Scope + "MCP.Access");
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OfflineAccess);

        foreach (var redirectUri in request.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri));
        }

        descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

        await appManager.CreateAsync(descriptor, ct);
        await db.SaveChangesAsync(ct);

        return Ok(new OAuthDynamicClientRegistrationResponse
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ClientIdIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ClientSecretExpiresAt = 0,
            RedirectUris = request.RedirectUris,
            GrantTypes = grantTypes,
            Scope = string.Join(' ', scopes),
            TokenEndpointAuthMethod = tokenMethod,
            ClientName = request.ClientName,
            SoftwareId = request.SoftwareId,
            SoftwareVersion = request.SoftwareVersion
        });
    }

    /// <summary>
    /// Handles authorization requests using Entra-authenticated employee identity.
    /// </summary>
    [HttpGet("/oauth/authorize")]
    [HttpPost("/oauth/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Request.Path + Request.QueryString
            };

            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        var entraObjectId = User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
            ?? User.FindFirstValue("oid");

        var tenantId = User.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid")
            ?? User.FindFirstValue("tid");

        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var appUser = await db.Users.AsNoTracking().SingleOrDefaultAsync(
            x => x.EntraObjectId == entraObjectId,
            ct);

        if (appUser is null || !appUser.IsActive)
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var requestedScopes = GetRequestedScopesFromAuthorizeRequest();

        var logger = HttpContext.RequestServices
            .GetRequiredService<ILogger<OAuthController>>();

        logger.LogInformation(
            "OAuth authorization requested scopes: {Scopes}",
            string.Join(", ", requestedScopes));

        logger.LogInformation(
            "OAuth authorization: Authenticated={Authenticated}, HasOid={HasOid}, HasTenant={HasTenant}",
            User.Identity?.IsAuthenticated,
            !string.IsNullOrWhiteSpace(entraObjectId),
            !string.IsNullOrWhiteSpace(tenantId));

        if (!requestedScopes.Contains("MCP.Access", StringComparer.Ordinal))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var grantedScopes = requestedScopes
            .Where(scope =>
                scope == "MCP.Access" ||
                scope == Scopes.OfflineAccess)
            .ToArray();

        var identity = new ClaimsIdentity(
            authenticationType: "OAuthAuthorizationServer",
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, entraObjectId);
        identity.SetClaim("oid", entraObjectId);

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            identity.SetClaim("tid", tenantId);
        }

        identity.SetClaim(Claims.Name, appUser.DisplayName);
        identity.SetClaim(Claims.Email, appUser.Email);
        identity.SetClaim(Claims.Private.Audience, "mcp");

        identity.SetScopes(grantedScopes);
        identity.SetResources("https://hq-githubmcp.bfsaul.net/mcp");

        var principal = new ClaimsPrincipal(identity);

        principal.SetScopes(grantedScopes);
        principal.SetResources("https://hq-githubmcp.bfsaul.net/mcp");

        principal.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject =>
                [Destinations.AccessToken],
            "oid" =>
                [Destinations.AccessToken],
            "tid" =>
                [Destinations.AccessToken],
            Claims.Name =>
                [Destinations.AccessToken],
            Claims.Email =>
                [Destinations.AccessToken],
            _ => Array.Empty<string>()
        });

        return SignIn(
            principal,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handles token exchange requests.
    /// </summary>
    [HttpPost("/oauth/token")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Exchange(CancellationToken ct)
    {
        var isAuthorizationCode = IsTokenRequestGrantType(GrantTypes.AuthorizationCode);
        var isRefreshToken = IsTokenRequestGrantType(GrantTypes.RefreshToken);

        if (isAuthorizationCode || isRefreshToken)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var principal = authenticateResult.Principal;

            var oid = principal.GetClaim("oid")
                ?? principal.GetClaim(Claims.Subject)
                ?? principal.GetClaim("http://schemas.microsoft.com/identity/claims/objectidentifier");

            if (string.IsNullOrWhiteSpace(oid))
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(
                x => x.EntraObjectId == oid,
                ct);

            if (user is null || !user.IsActive)
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new
        {
            error = Errors.UnsupportedGrantType,
            error_description = "Only authorization_code and refresh_token grant types are supported."
        });
    }

    /// <summary>
    /// Reads requested scopes from authorize request query/form payload.
    /// </summary>
    private HashSet<string> GetRequestedScopesFromAuthorizeRequest()
    {
        string? rawScopes = null;

        if (HttpMethods.IsPost(Request.Method) && Request.HasFormContentType)
        {
            rawScopes = Request.Form["scope"].ToString();
        }
        else
        {
            rawScopes = Request.Query["scope"].ToString();
        }

        if (string.IsNullOrWhiteSpace(rawScopes))
        {
            return [];
        }

        return rawScopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Determines whether token request grant_type matches expected value.
    /// </summary>
    private bool IsTokenRequestGrantType(string expectedGrantType)
    {
        if (!HttpMethods.IsPost(Request.Method) || !Request.HasFormContentType)
        {
            return false;
        }

        var grantType = Request.Form["grant_type"].ToString();

        return string.Equals(grantType, expectedGrantType, StringComparison.Ordinal);
    }
}
