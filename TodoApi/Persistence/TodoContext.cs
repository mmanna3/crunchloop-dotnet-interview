using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Models;

namespace TodoApi.Persistence;

public class TodoContext(DbContextOptions<TodoContext> options) : DbContext(options)
{
    public DbSet<TodoList> TodoList { get; set; } = default!;
    public DbSet<TodoItem> TodoItem { get; set; } = default!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<SyncEntity>())
        {
            if (entry.State is EntityState.Added)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State is EntityState.Modified)
            {
                // Skip UpdatedAt when only sync-tracking fields changed
                var hasBusinessChanges = entry.Properties.Any(p =>
                    p.IsModified
                    && p.Metadata.Name
                        is not (
                            nameof(SyncEntity.SyncedAt)
                            or nameof(SyncEntity.ExternalId)
                            or nameof(SyncEntity.UpdatedAt)
                        )
                );

                if (hasBusinessChanges)
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoList>().HasIndex(t => t.ExternalId);

        modelBuilder.Entity<TodoItem>().HasIndex(t => t.ExternalId);
    }
}
