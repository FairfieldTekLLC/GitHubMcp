using Microsoft.AspNetCore.Mvc;

namespace GitHubMCP.Controllers;

/// <summary>
/// Provides basic site navigation endpoints.
/// </summary>
public sealed class HomeController : Controller
{
    /// <summary>
    /// Returns the application landing page.
    /// </summary>
    /// <returns>The home page view.</returns>
    public IActionResult Index() => View();

    /// <summary>
    /// Returns the generic error page.
    /// </summary>
    /// <returns>The error page view.</returns>
    public IActionResult Error() => View();
}
