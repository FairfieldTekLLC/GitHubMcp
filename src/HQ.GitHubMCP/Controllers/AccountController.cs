using System.Security.Cryptography;
using System.Text;
using GitHubMCP.Data;
using GitHubMCP.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GitHubMCP.Controllers;

/// <summary>
/// Provides account endpoints for user profile and GitHub OAuth connection.
/// </summary>
public sealed class AccountController(
    AppDbContext db,
    UserRegistrationService users,
    GitHubService gh,
    IConfiguration cfg,
    IDataProtectionProvider dataProtectionProvider) : Controller
{
    /// <summary>
    /// Displays the current user profile.
    /// </summary>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The profile view.</returns>
    public async Task<IActionResult> Profile(CancellationToken ct) =>
        View(await users.CurrentAsync(User, ct));

    /// <summary>
    /// Starts the GitHub OAuth authorization flow.
    /// </summary>
    /// <returns>A redirect to GitHub authorization.</returns>
    public IActionResult ConnectGitHub()
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var protector = dataProtectionProvider
            .CreateProtector("GitHubMCP.GitHubOAuth.State.v1");

        var protectedState = protector.Protect(state);

        Response.Cookies.Append(
            "GitHubMCP.GitHubOAuth.State",
            protectedState,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10),
                IsEssential = true
            });

        var webBaseUrl = cfg["GitHub:WebBaseUrl"]
            ?? throw new InvalidOperationException("GitHub:WebBaseUrl is not configured.");

        var authorizeEndpoint = new Uri(new Uri(webBaseUrl), "login/oauth/authorize");

        var url =
            $"{authorizeEndpoint}" +
            $"?client_id={Uri.EscapeDataString(cfg["GitHub:ClientId"]!)}" +
            $"&redirect_uri={Uri.EscapeDataString(cfg["GitHub:CallbackUrl"]!)}" +
            $"&state={Uri.EscapeDataString(state)}";

        return Redirect(url);
    }

    /// <summary>
    /// Redirects the user to the GitHub organization SSO authorization page.
    /// </summary>
    /// <returns>A redirect to the GitHub organization SSO authorization page.</returns>
    public IActionResult CompleteGitHubSsoAuthorization()
    {
        var webBaseUrl = cfg["GitHub:WebBaseUrl"]
            ?? throw new InvalidOperationException("GitHub:WebBaseUrl is not configured.");

        var organization =
            cfg["GitHub:Organization"] ?? "B-F-Saul-Company";

        var ssoEndpoint = new Uri(
            new Uri(webBaseUrl),
            $"orgs/{Uri.EscapeDataString(organization)}/sso");

        return Redirect(ssoEndpoint.ToString());
    }

    /// <summary>
    /// Handles the GitHub OAuth callback and stores the connected GitHub identity.
    /// </summary>
    /// <param name="code">The OAuth authorization code.</param>
    /// <param name="state">The returned anti-forgery state value.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>A redirect to the profile view on success.</returns>
    [HttpGet("/github/callback")]
    public async Task<IActionResult> Callback(string code, string state, CancellationToken ct)
    {
        var protectedState = Request.Cookies["GitHubMCP.GitHubOAuth.State"];

        Response.Cookies.Delete("GitHubMCP.GitHubOAuth.State");

        if (string.IsNullOrWhiteSpace(protectedState) ||
            string.IsNullOrWhiteSpace(state))
        {
            return BadRequest("Invalid OAuth state.");
        }

        string expectedState;

        try
        {
            var protector = dataProtectionProvider
                .CreateProtector("GitHubMCP.GitHubOAuth.State.v1");

            expectedState = protector.Unprotect(protectedState);
        }
        catch (CryptographicException)
        {
            return BadRequest("Invalid OAuth state.");
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedState),
                Encoding.UTF8.GetBytes(state)))
        {
            return BadRequest("Invalid OAuth state.");
        }

        var u = await users.CurrentAsync(User, ct);
        var x = await gh.ExchangeCodeAsync(code, ct);
        var id = await db.GitHubIdentities.SingleOrDefaultAsync(i => i.AppUserId == u.Id, ct);

        if (id is null)
        {
            id = new() { AppUserId = u.Id };
            db.Add(id);
        }

        id.GitHubUserId = x.GitHubUserId;
        id.Login = x.Login;
        id.ProtectedAccessToken = gh.Protect(x.AccessToken);
        id.AccessTokenExpiresUtc = x.AccessTokenExpiresUtc;
        id.ProtectedRefreshToken =
            string.IsNullOrWhiteSpace(x.RefreshToken)
                ? null
                : gh.Protect(x.RefreshToken);
        id.RefreshTokenExpiresUtc = x.RefreshTokenExpiresUtc;
        id.ConnectedUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Profile));
    }
}
