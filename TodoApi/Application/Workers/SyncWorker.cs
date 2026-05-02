using Microsoft.Extensions.Options;
using TodoApi.Application.Services;

namespace TodoApi.Application.Workers;

public class SyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SyncSettings> options,
    ILogger<SyncWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PeriodicTimer requires a positive period; config might be 0 or missing
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervalSeconds));
        logger.LogInformation("Sync worker started — interval: {Interval}s", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope
                    .ServiceProvider.GetRequiredService<ISyncService>()
                    .SyncAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // SyncService already logged the error; swallow to keep the worker alive
                logger.LogWarning(
                    ex,
                    "Sync cycle threw an unhandled exception — worker will retry next interval"
                );
            }
        }
    }
}
