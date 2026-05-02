using Microsoft.EntityFrameworkCore;
using TodoApi.Application.ExternalApi;
using TodoApi.Persistence;

namespace TodoApi.Application.Services;

public class SyncService(
    IExternalTodoApiClient externalClient,
    TodoContext context,
    ILogger<SyncService> logger
) : ISyncService
{
    // Static so the lock is shared across scoped instances (worker + manual trigger)
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ListSyncer _syncer = new(externalClient, context, logger);

    public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
    {
        if (!await _semaphore.WaitAsync(0, ct))
        {
            logger.LogWarning("Sync already in progress; skipping this cycle");
            return SyncResult.Skipped();
        }

        try
        {
            return await RunSyncAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync cycle failed");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<SyncResult> RunSyncAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting sync cycle");
        var result = new SyncResult();

        var externalLists = await externalClient.GetAllAsync(ct);
        var localLists = await context.TodoList.Include(l => l.TodoItems).ToListAsync(ct);

        await _syncer.ReconcileAsync(localLists, externalLists, result, ct);

        // First save: persists all business changes; UpdatedAt auto-set by context override
        await context.SaveChangesAsync(ct);

        // Second save: set SyncedAt on all synced entities.
        // Because the context override skips UpdatedAt for sync-only field changes,
        // SyncedAt ends up > UpdatedAt, so these records won't be re-pushed next cycle.
        var syncedAt = DateTime.UtcNow;
        foreach (var list in context.TodoList.Local.Where(l => l.ExternalId != null))
            list.SyncedAt = syncedAt;
        foreach (var item in context.TodoItem.Local.Where(i => i.ExternalId != null))
            item.SyncedAt = syncedAt;
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Sync complete — local: +{LC} ~{LU} -{LD} | external: +{EC} ~{EU} | items local: +{LIC} ~{LIU} -{LID} | items ext: ~{EIU}",
            result.LocalListsCreated,
            result.LocalListsUpdated,
            result.LocalListsDeleted,
            result.ExternalListsCreated,
            result.ExternalListsUpdated,
            result.LocalItemsCreated,
            result.LocalItemsUpdated,
            result.LocalItemsDeleted,
            result.ExternalItemsUpdated
        );

        return result;
    }
}
