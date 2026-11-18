using GitHubMCP.Models;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore;

namespace GitHubMCP.Data;

/// <summary>
/// Entity Framework Core context for GitHub MCP application and OAuth data.
/// </summary>
/// <param name="options">The options used to configure the context.</param>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the application users.
    /// </summary>
    public DbSet<AppUser> Users => Set<AppUser>();

    /// <summary>
    /// Gets connected GitHub identities.
    /// </summary>
    public DbSet<GitHubIdentity> GitHubIdentities => Set<GitHubIdentity>();

    /// <summary>
    /// Gets registered GitHub repositories.
    /// </summary>
    public DbSet<GitHubRepository> Repositories => Set<GitHubRepository>();

    /// <summary>
    /// Gets repository access grants.
    /// </summary>
    public DbSet<RepositoryGrant> RepositoryGrants => Set<RepositoryGrant>();

    /// <summary>
    /// Gets approval requests.
    /// </summary>
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    /// <summary>
    /// Gets audit log entries.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Gets dynamically registered OAuth clients.
    /// </summary>
    public DbSet<OAuthClientRegistration> OAuthClientRegistrations =>
        Set<OAuthClientRegistration>();

    /// <summary>
    /// Configures model indexes and OAuth entity mappings.
    /// </summary>
    /// <param name="b">The model builder used to configure entities.</param>
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.UseOpenIddict();

        b.Entity<AppUser>().HasIndex(x => x.EntraObjectId).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.Email);
        b.Entity<GitHubIdentity>().HasIndex(x => x.AppUserId).IsUnique();
        b.Entity<GitHubRepository>().HasIndex(x => new { x.Owner, x.Name }).IsUnique();
        b.Entity<RepositoryGrant>().HasIndex(x => new { x.AppUserId, x.GitHubRepositoryId }).IsUnique();

        b.Entity<ApprovalRequest>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.RequestedBy)
                .WithMany()
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DecidedBy)
                .WithMany()
                .HasForeignKey(x => x.DecidedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Repository)
                .WithMany()
                .HasForeignKey(x => x.RepositoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.RequestedByUserId);
            entity.HasIndex(x => x.DecidedByUserId);
            entity.HasIndex(x => x.RepositoryId);

            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<AuditLog>().HasIndex(x => x.CreatedUtc);
        b.Entity<OAuthClientRegistration>().HasIndex(x => x.ClientId).IsUnique();
    }
}
