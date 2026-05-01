using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TodoApi.Api.Hubs;
using TodoApi.Application;
using TodoApi.Application.Dtos;
using TodoApi.Application.Workers;
using TodoApi.Domain.Repositories;

namespace TodoApi.Tests;

public class CompleteAllItemsWorkerTest
{
    private static IOptions<WorkerSettings> FastWorkerOptions() =>
        Options.Create(new WorkerSettings { BatchSize = 2, DelayMilliseconds = 0 });

    private static (
        Mock<ITodoItemsRepository> Repository,
        Mock<IHubClients> HubClients,
        Mock<ISingleClientProxy> ClientProxy,
        CompleteAllItemsWorker Worker,
        Channel<CompleteAllItemsJob> Channel
    ) CreateSut()
    {
        var repository = new Mock<ITodoItemsRepository>(MockBehavior.Strict);
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

        serviceProvider
            .Setup(sp => sp.GetService(typeof(ITodoItemsRepository)))
            .Returns(repository.Object);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var clientProxy = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        var hubClients = new Mock<IHubClients>(MockBehavior.Strict);
        hubClients.Setup(clients => clients.Client(It.IsAny<string>())).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<TodoHub>>(MockBehavior.Strict);
        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

        var channel = Channel.CreateUnbounded<CompleteAllItemsJob>();

        var worker = new CompleteAllItemsWorker(
            channel,
            factory.Object,
            hubContext.Object,
            FastWorkerOptions()
        );

        return (repository, hubClients, clientProxy, worker, channel);
    }

    private static async Task RunJobAsync(
        CompleteAllItemsWorker worker,
        Channel<CompleteAllItemsJob> channel,
        CompleteAllItemsJob job
    )
    {
        await worker.StartAsync(CancellationToken.None);

        await channel.Writer.WriteAsync(job);
        channel.Writer.TryComplete();

        if (worker.ExecuteTask != null)
        {
            await worker.ExecuteTask;
        }

        await worker.StopAsync(CancellationToken.None);
    }

    private static void SetupOperationProgressCapture(
        Mock<ISingleClientProxy> proxy,
        List<CompleteAllProgressDTO> history
    )
    {
        proxy
            .Setup(p =>
                p.SendCoreAsync(
                    "OperationProgress",
                    It.Is<object?[]>(a => a != null && a.Length == 1),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask)
            .Callback<string, object?[], CancellationToken>(
                (_, args, _) =>
                {
                    if (args[0] is CompleteAllProgressDTO dto)
                        history.Add(dto);
                }
            );
    }

    [Fact]
    public async Task ProcessJob_MultipleBatches_UpdatesProgressGradually()
    {
        var (repo, _, clientProxy, worker, channel) = CreateSut();
        var operationId = Guid.NewGuid();
        const string connId = "conn-1";
        const long listId = 42L;

        repo.SetupSequence(r => r.GetIncompleteItemIdsBatchAsync(listId, 2))
            .ReturnsAsync([10L, 11L])
            .ReturnsAsync([12L, 13L])
            .ReturnsAsync([14L])
            .ReturnsAsync([]);

        repo.Setup(r => r.CompleteItemsByIdsAsync(It.IsAny<IReadOnlyList<long>>()))
            .Returns(Task.CompletedTask);

        var history = new List<CompleteAllProgressDTO>();
        SetupOperationProgressCapture(clientProxy, history);

        var job = new CompleteAllItemsJob
        {
            OperationId = operationId,
            ListId = listId,
            Total = 5,
            ConnectionId = connId,
        };

        await RunJobAsync(worker, channel, job);

        repo.Verify(
            r => r.CompleteItemsByIdsAsync(It.IsAny<IReadOnlyList<long>>()),
            Times.Exactly(3)
        );

        Assert.Equal(3, history.Count);
        Assert.Equal(2, history[0].Completed);
        Assert.Equal(4, history[1].Completed);
        Assert.Equal(5, history[2].Completed);
        Assert.Equal(operationId.ToString(), history.Last().OperationId);
    }

    [Fact]
    public async Task ProcessJob_WhenNoItems_SendsTerminalMessage()
    {
        var (repo, _, clientProxy, worker, channel) = CreateSut();
        repo.Setup(r => r.GetIncompleteItemIdsBatchAsync(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync([]);

        var history = new List<CompleteAllProgressDTO>();
        SetupOperationProgressCapture(clientProxy, history);

        var job = new CompleteAllItemsJob
        {
            OperationId = Guid.NewGuid(),
            ListId = 1,
            Total = 10,
            ConnectionId = "abc",
        };

        await RunJobAsync(worker, channel, job);

        var finalUpdate = Assert.Single(history);
        Assert.Equal(10, finalUpdate.Completed);
        Assert.Equal(10, finalUpdate.Total);
        Assert.Empty(finalUpdate.CompletedIds);
    }

    [Fact]
    public async Task ProcessJob_TotalZero_SendsEmptyProgressDto()
    {
        var (repo, _, clientProxy, worker, channel) = CreateSut();
        repo.Setup(r => r.GetIncompleteItemIdsBatchAsync(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync([]);

        var history = new List<CompleteAllProgressDTO>();
        SetupOperationProgressCapture(clientProxy, history);

        var job = new CompleteAllItemsJob
        {
            OperationId = Guid.NewGuid(),
            ListId = 1,
            Total = 0,
            ConnectionId = "zero-case",
        };

        await RunJobAsync(worker, channel, job);

        var finalUpdate = Assert.Single(history);
        Assert.Equal(0, finalUpdate.Completed);
        Assert.Equal(0, finalUpdate.Total);
    }
}
