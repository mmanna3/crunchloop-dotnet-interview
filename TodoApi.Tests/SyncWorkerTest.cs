using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TodoApi.Application;
using TodoApi.Application.Services;
using TodoApi.Application.Workers;

namespace TodoApi.Tests;

public class SyncWorkerTest
{
    private static IOptions<SyncSettings> FastIntervalOptions(int intervalSeconds = 1) =>
        Options.Create(new SyncSettings { IntervalSeconds = intervalSeconds });

    private static (
        Mock<ISyncService> Sync,
        Mock<IServiceScopeFactory> ScopeFactory,
        SyncWorker Worker
    ) CreateSut(IOptions<SyncSettings>? options = null)
    {
        var syncService = new Mock<ISyncService>(MockBehavior.Strict);
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider
            .Setup(sp => sp.GetService(typeof(ISyncService)))
            .Returns(syncService.Object);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var worker = new SyncWorker(
            factory.Object,
            options ?? FastIntervalOptions(),
            NullLogger<SyncWorker>.Instance
        );
        return (syncService, factory, worker);
    }

    /// <summary>
    /// First <see cref="PeriodicTimer"/> tick fires after the first interval (not immediately).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_PeriodicallyInvokesSync()
    {
        var (sync, _, worker) = CreateSut();
        sync.Setup(s => s.SyncAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SyncResult());

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        sync.Verify(s => s.SyncAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSyncThrows_ContinuesAndInvokesSyncAgain()
    {
        var (sync, _, worker) = CreateSut();
        sync
            .SetupSequence(s => s.SyncAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"))
            .ReturnsAsync(new SyncResult());

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(2.5));
        await worker.StopAsync(CancellationToken.None);

        sync.Verify(s => s.SyncAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesScopePerTick()
    {
        var (sync, scopeFactory, worker) = CreateSut();
        sync.Setup(s => s.SyncAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SyncResult());

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        scopeFactory.Verify(f => f.CreateScope(), Times.AtLeastOnce());
    }
}
