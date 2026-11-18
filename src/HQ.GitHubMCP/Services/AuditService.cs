using System.Text.Json;
using GitHubMCP.Data;
using GitHubMCP.Models;

namespace GitHubMCP.Services;

/// <summary>
/// Writes audit records for security-sensitive and approval-related operations.
/// </summary>
/// <param name="db">The application data context.</param>
public sealed class AuditService(AppDbContext db)
{
    /// <summary>
    /// Writes a single audit log entry.
    /// </summary>
    /// <param name="uid">The acting user identifier when available.</param>
    /// <param name="action">The action name.</param>
    /// <param name="type">The entity type involved.</param>
    /// <param name="id">The entity identifier.</param>
    /// <param name="outcome">The operation outcome.</param>
    /// <param name="detail">Optional structured detail payload.</param>
    /// <param name="ct">A token used to cancel the operation.</param>
    /// <returns>A task that completes when the entry is persisted.</returns>
    public async Task WriteAsync(Guid? uid, string action, string type, string id, string outcome, object? detail, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            AppUserId = uid,
            Action = action,
            EntityType = type,
            EntityId = id,
            Outcome = outcome,
            DetailsJson = JsonSerializer.Serialize(detail ?? new { })
        });

        await db.SaveChangesAsync(ct);
    }
}
