using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TodoApi.Api.Hubs;
using TodoApi.Application.Dtos;
using TodoApi.Domain.Repositories;

namespace TodoApi.Application.Workers;

public class CompleteAllItemsJob
{
    public required Guid OperationId { get; set; }
    public required long ListId { get; set; }
    public required int Total { get; set; }
    public required string ConnectionId { get; set; }
}

public class CompleteAllItemsWorker(
    Channel<CompleteAllItemsJob> channel,
    IServiceScopeFactory scopeFactory,
    IHubContext<TodoHub> hubContext,
    IOptions<WorkerSettings> workerSettings
) : BackgroundService
{
    private readonly WorkerSettings _settings = workerSettings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken))
            await ProcessJobAsync(job, stoppingToken);
    }

    private async Task ProcessJobAsync(CompleteAllItemsJob job, CancellationToken ct)
    {
        var operationIdStr = job.OperationId.ToString();
        var processed = 0;

        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITodoItemsRepository>();

        while (!ct.IsCancellationRequested)
        {
            var ids = await repo.GetIncompleteItemIdsBatchAsync(job.ListId, _settings.BatchSize);
            if (ids.Count == 0)
                break;

            await repo.CompleteItemsByIdsAsync(ids);
            processed += ids.Count;

            await SendProgressAsync(
                job.ConnectionId,
                operationIdStr,
                job.ListId,
                processed,
                job.Total,
                ids,
                ct
            );

            if (_settings.DelayMilliseconds > 0)
                await Task.Delay(_settings.DelayMilliseconds, ct);
        }

        if (processed < job.Total || job.Total == 0)
            await SendProgressAsync(
                job.ConnectionId,
                operationIdStr,
                job.ListId,
                job.Total,
                job.Total,
                [],
                ct
            );
    }

    private Task SendProgressAsync(
        string connectionId,
        string operationId,
        long listId,
        int completed,
        int total,
        IReadOnlyList<long> completedIds,
        CancellationToken ct
    ) =>
        hubContext
            .Clients.Client(connectionId)
            .SendAsync(
                "OperationProgress",
                new CompleteAllProgressDTO
                {
                    OperationId = operationId,
                    ListId = listId,
                    Completed = completed,
                    Total = total,
                    CompletedIds = completedIds,
                },
                ct
            );
}
