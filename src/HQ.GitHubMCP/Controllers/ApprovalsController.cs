using GitHubMCP.Data;
using GitHubMCP.Models;
using GitHubMCP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GitHubMCP.Controllers;

/// <summary>
/// Provides endpoints for reviewing and executing approval-gated repository operations.
/// </summary>
public sealed class ApprovalsController(
    AppDbContext db,
    UserRegistrationService users,
    ApprovalService approvals) : Controller
{
    /// <summary>
    /// Lists approval requests visible to the current user.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var u = await users.CurrentAsync(User, ct);

        var query = db.ApprovalRequests
            .Include(x => x.Repository)
            .AsQueryable();

        if (u.Role != UserRole.Administrator || !u.IsActive)
        {
            query = query.Where(x => x.RequestedByUserId == u.Id);
        }

        return View(await query
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(ct));
    }

    /// <summary>
    /// Shows a specific approval request for administrator review.
    /// </summary>
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Review(Guid id, CancellationToken ct)
    {
        var user = await users.CurrentAsync(User, ct);

        if (!user.IsActive || user.Role != UserRole.Administrator)
        {
            return Forbid();
        }

        var request = await db.ApprovalRequests
            .Include(x => x.Repository)
            .SingleOrDefaultAsync(x => x.Id == id, ct);

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != ApprovalStatus.Pending ||
            request.ExpiresUtc <= DateTimeOffset.UtcNow)
        {
            return BadRequest("Approval request is no longer pending.");
        }

        return View(request);
    }

    /// <summary>
    /// Records an administrator approval or rejection decision.
    /// </summary>
    [Authorize(Policy = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(Guid id, bool approve, CancellationToken ct)
    {
        var user = await users.CurrentAsync(User, ct);

        if (!user.IsActive || user.Role != UserRole.Administrator)
        {
            return Forbid();
        }

        var request = await db.ApprovalRequests
            .SingleOrDefaultAsync(x => x.Id == id, ct);

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != ApprovalStatus.Pending ||
            request.ExpiresUtc <= DateTimeOffset.UtcNow)
        {
            return BadRequest("Approval request is no longer pending.");
        }

        await approvals.DecideAsync(id, user, approve, ct);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Executes a previously approved request.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(Guid id, CancellationToken ct)
    {
        var u = await users.CurrentAsync(User, ct);
        await approvals.ExecuteAsync(id, u, ct);
        return RedirectToAction(nameof(Index));
    }
}
