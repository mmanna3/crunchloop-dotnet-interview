namespace TodoApi.Application.Services;

public interface ISyncService
{
    Task<SyncResult> SyncAsync(CancellationToken ct = default);
}
