namespace TodoApi.Application.Services;

public static class SyncDecision
{
    public static (bool push, bool pull) Resolve(
        DateTime? localUpdatedAt,
        DateTime? syncedAt,
        DateTime externalUpdatedAt
    )
    {
        var localChanged = localUpdatedAt > syncedAt;
        var externalChanged = syncedAt.HasValue && externalUpdatedAt > syncedAt;

        if (!localChanged && !externalChanged)
            return (push: false, pull: false);
        if (localChanged && !externalChanged)
            return (push: true, pull: false);
        if (!localChanged && externalChanged)
            return (push: false, pull: true);

        // Both changed since last sync: last-write wins
        return localUpdatedAt >= externalUpdatedAt
            ? (push: true, pull: false)
            : (push: false, pull: true);
    }
}
