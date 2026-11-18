using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using GitHubMCP.Data;
using GitHubMCP.Middleware;
using GitHubMCP.Models;
using GitHubMCP.Security;
using GitHubMCP.Services;
using GitHubMCP.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using ModelContextProtocol.AspNetCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        .ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services
    .AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<AppDbContext>();
    })
    .AddServer(options =>
    {
        options.SetIssuer(new Uri(
            builder.Configuration["OAuth:Issuer"] ??
            "https://hq-githubmcp.bfsaul.net/"));

        options.SetAuthorizationEndpointUris("/oauth/authorize");
        options.SetTokenEndpointUris("/oauth/token");

        options.AddEventHandler<
            OpenIddictServerEvents.HandleConfigurationRequestContext>(handler => handler.UseInlineHandler(context =>
        {
            var issuer = (builder.Configuration["OAuth:Issuer"] ??
                          "https://hq-githubmcp.bfsaul.net/")
                .TrimEnd('/') + "/";

            context.Metadata["registration_endpoint"] =
                issuer + "oauth/register";

            return default;
        }));

        options.AllowAuthorizationCodeFlow();
        options.AllowRefreshTokenFlow();
        options.RequireProofKeyForCodeExchange();

        options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

        // Enforce PKCE S256 only (also removes "plain" from discovery).
        options.Configure(o => o.CodeChallengeMethods.Remove(
            OpenIddictConstants.CodeChallengeMethods.Plain));

        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.OfflineAccess,
            "MCP.Access");

        var signingThumbprint =
            builder.Configuration["OAuth:SigningCertificateThumbprint"];

        var encryptionThumbprint =
            builder.Configuration["OAuth:EncryptionCertificateThumbprint"];

        if (string.IsNullOrWhiteSpace(signingThumbprint) ||
            string.IsNullOrWhiteSpace(encryptionThumbprint))
            throw new InvalidOperationException(
                "OAuth certificate thumbprints must be configured.");

        using var store = new X509Store(
            StoreName.My,
            StoreLocation.LocalMachine);

        store.Open(OpenFlags.ReadOnly);

        var signingCert = store.Certificates
                              .Find(
                                  X509FindType.FindByThumbprint,
                                  signingThumbprint,
                                  false)
                              .OfType<X509Certificate2>()
                              .SingleOrDefault()
                          ?? throw new InvalidOperationException(
                              "OAuth signing certificate not found.");

        var encryptionCert = store.Certificates
                                 .Find(
                                     X509FindType.FindByThumbprint,
                                     encryptionThumbprint,
                                     false)
                                 .OfType<X509Certificate2>()
                                 .SingleOrDefault()
                             ?? throw new InvalidOperationException(
                                 "OAuth encryption certificate not found.");

        if (!signingCert.HasPrivateKey ||
            !encryptionCert.HasPrivateKey)
            throw new InvalidOperationException(
                "OAuth certificates must contain accessible private keys.");

        options.AddSigningCertificate(signingCert);
        options.AddEncryptionCertificate(encryptionCert);

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// ============================================================
// Microsoft Entra ID Authentication
// Certificate-based authentication
// ============================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            OpenIdConnectDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("AzureAd"));

builder.Services
    .AddAuthentication()
    .AddMicrosoftIdentityWebApi(
        builder.Configuration.GetSection("AzureAd"),
        JwtBearerDefaults.AuthenticationScheme);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;

    options.AddPolicy("Administrator", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(
            new AdministratorRequirement());
    });

    options.AddPolicy("McpBearer", policy =>
    {
        policy.AddAuthenticationSchemes(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var scopeValues = context.User
                .FindAll("scope")
                .SelectMany(c => c.Value.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

            return scopeValues.Contains("MCP.Access", StringComparer.Ordinal);
        });
    });
});

builder.Services.AddSingleton<
    IAuthorizationHandler,
    DatabaseAdministratorHandler>();

builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddRazorPages();
builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<GitHubService>();

builder.Services.AddScoped<UserRegistrationService>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<RepositoryAccessService>();
builder.Services.AddScoped<GitHubTools>();
builder.Services.AddScoped<GitHubToolExecutor>();
builder.Services.AddScoped<GitHubRepoBrowseTools>();
builder.Services.AddScoped<GitHubRepoWriteTools>();
builder.Services.AddScoped<GitHubPullRequestIssueTools>();
builder.Services.AddScoped<GitHubActionsTools>();
builder.Services.AddScoped<GitHubSecurityGovernanceTools>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<GitHubTools>()
    .WithTools<GitHubRepoBrowseTools>()
    .WithTools<GitHubRepoWriteTools>()
    .WithTools<GitHubPullRequestIssueTools>()
    .WithTools<GitHubActionsTools>()
    .WithTools<GitHubSecurityGovernanceTools>();

var app = builder.Build();

// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//
//     var entityType = db.Model.FindEntityType(typeof(ApprovalRequest))
//         ?? throw new InvalidOperationException(
//             "ApprovalRequest is missing from the EF model.");
//
//     var properties = entityType.GetProperties()
//         .Select(x => x.Name)
//         .OrderBy(x => x)
//         .ToArray();
//
//     app.Logger.LogInformation(
//         "ApprovalRequest EF properties: {Properties}",
//         string.Join(", ", properties));
//
//     if (properties.Contains(
//             "RequestedById",
//             StringComparer.Ordinal))
//     {
//         throw new InvalidOperationException(
//             "EF model still contains unintended RequestedById property.");
//     }
//
//     try
//     {
//         db.Database.Migrate();
//     }
//     catch (InvalidOperationException ex)
//         when (ex.Message.Contains(
//             "PendingModelChangesWarning",
//             StringComparison.Ordinal))
//     {
//         app.Logger.LogWarning(
//             ex,
//             "Skipping automatic migration because pending model changes were detected.");
//     }
// }

var oidcOptions = app.Services
    .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
    .Get(OpenIdConnectDefaults.AuthenticationScheme);

app.Logger.LogInformation(
    "OIDC ResponseType: {ResponseType}",
    oidcOptions.ResponseType);

app.Logger.LogInformation(
    "OIDC ClientId configured: {Configured}",
    !string.IsNullOrWhiteSpace(oidcOptions.ClientId));

app.Logger.LogInformation(
    "OIDC CodeReceivedHandler configured: {Configured}",
    oidcOptions.Events.OnAuthorizationCodeReceived != null);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

// Temporarily disabled for Entra callback loop isolation.
app.UseMiddleware<UserRegistrationMiddleware>();

app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();
app.MapRazorPages();

// ============================================================
// OAuth 2.1 Protected Resource Metadata
// MCP authorization discovery
// ============================================================

app.MapGet(
        "/.well-known/oauth-protected-resource/mcp",
        (IConfiguration configuration) =>
        {
            var issuer = (configuration["OAuth:Issuer"] ??
                          "https://hq-githubmcp.bfsaul.net/")
                .TrimEnd('/') + "/";

            return Results.Json(new
            {
                resource =
                    "https://hq-githubmcp.bfsaul.net/mcp",

                authorization_servers = new[]
                {
                    issuer
                },

                scopes_supported = new[]
                {
                    "MCP.Access",
                    OpenIddictConstants.Scopes.OfflineAccess
                },

                bearer_methods_supported = new[]
                {
                    "header"
                }
            });
        })
    .AllowAnonymous();

app.MapGet(
        "/.well-known/oauth-protected-resource",
        (IConfiguration configuration) =>
        {
            var issuer = (configuration["OAuth:Issuer"] ??
                          "https://hq-githubmcp.bfsaul.net/")
                .TrimEnd('/') + "/";

            return Results.Json(new
            {
                resource =
                    "https://hq-githubmcp.bfsaul.net/mcp",

                authorization_servers = new[]
                {
                    issuer
                },

                scopes_supported = new[]
                {
                    "MCP.Access",
                    OpenIddictConstants.Scopes.OfflineAccess
                }
            });
        })
    .AllowAnonymous();

// ============================================================
// OAuth 2.1 Dynamic Client Registration (RFC 7591)
// ============================================================

app.MapPost(
        "/oauth/register",
        async (
            HttpContext context,
            IOpenIddictApplicationManager applicationManager,
            CancellationToken cancellationToken) =>
        {
            static IResult Error(string error, string description)
            {
                return Results.Json(
                    new
                    {
                        error,
                        error_description = description
                    },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            DynamicClientRegistrationRequest? request;

            try
            {
                request = await context.Request
                    .ReadFromJsonAsync<DynamicClientRegistrationRequest>(
                        cancellationToken);
            }
            catch (Exception ex) when (
                ex is JsonException or InvalidOperationException)
            {
                return Error(
                    "invalid_client_metadata",
                    "The registration request is not valid JSON.");
            }

            if (request is null)
                return Error(
                    "invalid_client_metadata",
                    "The registration request body is required.");

            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("OAuth.DynamicRegistration");

            logger.LogInformation(
                "DCR requested authentication method: {Method}",
                request.TokenEndpointAuthMethod ?? "(not supplied)");

            logger.LogInformation(
                "DCR requested grant types: {GrantTypes}",
                string.Join(", ",
                    request.GrantTypes ?? Array.Empty<string>()));

            if (request.RedirectUris is null ||
                request.RedirectUris.Length == 0)
                return Error(
                    "invalid_redirect_uri",
                    "At least one redirect_uri is required.");

            var normalizedRedirectUris = new List<string>();

            foreach (var value in request.RedirectUris)
            {
                if (!Uri.TryCreate(
                        value,
                        UriKind.Absolute,
                        out var redirectUri))
                    return Error(
                        "invalid_redirect_uri",
                        "Every redirect_uri must be an absolute URI.");

                if (!string.Equals(
                        redirectUri.Scheme,
                        Uri.UriSchemeHttps,
                        StringComparison.OrdinalIgnoreCase))
                    return Error(
                        "invalid_redirect_uri",
                        "Only HTTPS redirect URIs are permitted.");

                if (!string.Equals(
                        redirectUri.Host,
                        "openwebui.bfsaul.net",
                        StringComparison.OrdinalIgnoreCase))
                    return Error(
                        "invalid_redirect_uri",
                        "The redirect URI host is not approved.");

                if (!string.IsNullOrEmpty(redirectUri.Fragment))
                    return Error(
                        "invalid_redirect_uri",
                        "Redirect URIs cannot contain fragments.");

                normalizedRedirectUris.Add(redirectUri.AbsoluteUri);
            }

            var requestedGrantTypes =
                request.GrantTypes?.Length > 0
                    ? request.GrantTypes
                    : new[] { "authorization_code", "refresh_token" };

            if (requestedGrantTypes.Any(value =>
                    value != "authorization_code" &&
                    value != "refresh_token"))
                return Error(
                    "invalid_client_metadata",
                    "Only authorization_code and refresh_token are supported.");

            var requestedResponseTypes =
                request.ResponseTypes?.Length > 0
                    ? request.ResponseTypes
                    : new[] { "code" };

            if (requestedResponseTypes.Any(value => value != "code"))
                return Error(
                    "invalid_client_metadata",
                    "Only the code response type is supported.");

            var requestedScopes =
                string.IsNullOrWhiteSpace(request.Scope)
                    ? new[]
                    {
                        "MCP.Access",
                        OpenIddictConstants.Scopes.OfflineAccess
                    }
                    : request.Scope.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

            if (requestedScopes.Any(scope =>
                    !string.Equals(scope, "MCP.Access", StringComparison.Ordinal) &&
                    !string.Equals(scope, OpenIddictConstants.Scopes.OfflineAccess, StringComparison.Ordinal)))
                return Error(
                    OpenIddictConstants.Errors.InvalidScope,
                    "Only MCP.Access and offline_access scopes are supported.");

            var authMethod =
                string.IsNullOrWhiteSpace(request.TokenEndpointAuthMethod)
                    ? "client_secret_basic"
                    : request.TokenEndpointAuthMethod;

            if (authMethod != "none" &&
                authMethod != "client_secret_post" &&
                authMethod != "client_secret_basic")
                return Error(
                    "invalid_client_metadata",
                    "Unsupported token endpoint authentication method.");

            var isPublicClient = authMethod == "none";

            var clientId = Guid.NewGuid().ToString("N");

            var clientSecret = isPublicClient
                ? null
                : Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(48));

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientType = isPublicClient
                    ? OpenIddictConstants.ClientTypes.Public
                    : OpenIddictConstants.ClientTypes.Confidential,
                ClientSecret = clientSecret,
                ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                DisplayName = string.IsNullOrWhiteSpace(request.ClientName)
                    ? "Open WebUI MCP Client"
                    : request.ClientName
            };

            foreach (var redirectUri in normalizedRedirectUris) descriptor.RedirectUris.Add(new Uri(redirectUri));

            descriptor.Permissions.Add(
                OpenIddictConstants.Permissions.Endpoints.Authorization);

            descriptor.Permissions.Add(
                OpenIddictConstants.Permissions.Endpoints.Token);

            descriptor.Permissions.Add(
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);

            descriptor.Permissions.Add(
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

            descriptor.Permissions.Add(
                OpenIddictConstants.Permissions.ResponseTypes.Code);

            descriptor.Permissions.Add(
                OpenIddictConstants.Permissions.Prefixes.Scope +
                "MCP.Access");

            // Required for the client to request refresh tokens.
            descriptor.Permissions.Add(
                OpenIddictConstants.Permissions.Prefixes.Scope +
                OpenIddictConstants.Scopes.OfflineAccess);

            descriptor.Requirements.Add(
                OpenIddictConstants.Requirements.Features
                    .ProofKeyForCodeExchange);

            await applicationManager.CreateAsync(
                descriptor,
                cancellationToken);

            return Results.Json(
                new
                {
                    client_id = clientId,
                    client_secret = clientSecret,
                    client_id_issued_at =
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    client_name = descriptor.DisplayName,
                    redirect_uris = normalizedRedirectUris,
                    grant_types = requestedGrantTypes,
                    response_types = requestedResponseTypes,
                    scope = string.Join(' ', requestedScopes),
                    token_endpoint_auth_method = authMethod
                },
                statusCode: StatusCodes.Status201Created);
        })
    .AllowAnonymous()
    .DisableAntiforgery();

app.MapMcp("/mcp")
    .RequireAuthorization("McpBearer");

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    utc = DateTimeOffset.UtcNow
})).AllowAnonymous();

app.MapGet("/auth-test", (HttpContext context) =>
        Results.Ok(new
        {
            IsAuthenticated = context.User.Identity?.IsAuthenticated ?? false,
            context.User.Identity?.AuthenticationType,
            context.User.Identity?.Name,
            Claims = context.User.Claims.Select(c => new { c.Type, c.Value })
        }))
    .RequireAuthorization();

app.Run();