using GitHubMCP.Data;
using GitHubMCP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GitHubMCP.Controllers;

/// <summary>
/// Provides administrator endpoints for managing registered GitHub repositories.
/// </summary>
[Authorize(Policy = "Administrator")]
public sealed class RepositoriesController(AppDbContext db) : Controller
{
    /// <summary>
    /// Lists all registered repositories.
    /// </summary>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>The repository list view.</returns>
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await db.Repositories.OrderBy(x => x.Owner).ThenBy(x => x.Name).ToListAsync(ct));

    /// <summary>
    /// Returns the repository creation form.
    /// </summary>
    /// <returns>The repository create view.</returns>
    public IActionResult Create() => View(new RepositoryViewModel());

    /// <summary>
    /// Creates a registered repository from form input.
    /// </summary>
    /// <param name="m">The submitted repository model.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>A redirect to the index view on success; otherwise the create view.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RepositoryViewModel m, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(m);

        db.Add(new GitHubRepository
        {
            Owner = m.Owner,
            Name = m.Name,
            DefaultBranch = m.DefaultBranch
        });

        await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index));
    }
}
