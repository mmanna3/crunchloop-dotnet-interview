using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.Application.ExternalApi;
using TodoApi.Application.ExternalApi.Dtos;
using TodoApi.Application.Services;
using TodoApi.Domain.Models;
using TodoApi.Persistence;
using TodoApi.Tests.Helpers;

namespace TodoApi.Tests.IntegrationTests;

public class SyncServiceIT
{
    // Shared baseline for “last sync” in conflict scenarios
    private static readonly DateTime UtcT = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static WebApplicationFactory<Program> CreateFactory(
        FakeExternalTodoApiClient fakeClient,
        string dbName
    ) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<TodoContext>));
                services.AddDbContext<TodoContext>(o => o.UseInMemoryDatabase(dbName));
                services.RemoveAll<IExternalTodoApiClient>();
                services.AddSingleton<IExternalTodoApiClient>(fakeClient);
            })
        );

    private sealed class SyncTestSession : IAsyncDisposable
    {
        public required WebApplicationFactory<Program> Factory { get; init; }
        public required IServiceScope Scope { get; init; }
        public required SyncResult Result { get; init; }
        public TodoContext Db => Scope.ServiceProvider.GetRequiredService<TodoContext>();

        public async ValueTask DisposeAsync()
        {
            Scope.Dispose();
            await Factory.DisposeAsync();
        }
    }

    private static async Task<SyncTestSession> RunSyncAsync(
        Action<TodoContext>? seed = null,
        FakeExternalTodoApiClient? fakeClient = null
    )
    {
        fakeClient ??= new FakeExternalTodoApiClient();
        var dbName = Guid.NewGuid().ToString();
        var factory = CreateFactory(fakeClient, dbName);

        using (var seedScope = factory.Services.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<TodoContext>();
            ctx.Database.EnsureCreated();
            seed?.Invoke(ctx);
            ctx.SaveChanges(); // sync SaveChanges bypasses the UpdatedAt override — lets us set arbitrary timestamps
        }

        var scope = factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISyncService>().SyncAsync();
        return new SyncTestSession
        {
            Factory = factory,
            Scope = scope,
            Result = result,
        };
    }

    [Fact]
    public async Task Sync_WhenLocalListHasNoExternalId_PushesToExternalAndLinksId()
    {
        var fake = new FakeExternalTodoApiClient();
        await using var session = await RunSyncAsync(
            seed: ctx => ctx.TodoList.Add(new TodoList { Name = "My List" }),
            fakeClient: fake
        );

        var list = session.Db.TodoList.Single();

        Assert.Single(fake.CreatedLists);
        Assert.Equal("My List", fake.CreatedLists[0].Name);
        Assert.NotNull(list.ExternalId);
        Assert.Equal(1, session.Result.ExternalListsCreated);
    }

    [Fact]
    public async Task Sync_WhenExternalHasUnknownList_CreatesItLocally()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-99",
                    Name = "External Only",
                    UpdatedAt = DateTime.UtcNow,
                    Items = [],
                },
            ],
        };

        await using var session = await RunSyncAsync(fakeClient: fake);

        var list = session.Db.TodoList.Single();

        Assert.Equal("External Only", list.Name);
        Assert.Equal("ext-99", list.ExternalId);
        Assert.Equal(1, session.Result.LocalListsCreated);
    }

    [Fact]
    public async Task Sync_WhenLocalModifiedAfterLastSync_UpdatesExternal()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "Old Name",
                    UpdatedAt = UtcT, // external not changed since last sync
                    Items = [],
                },
            ],
        };

        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "New Name", ExternalId = "ext-1" };
                list.SyncedAt = UtcT;
                list.UpdatedAt = UtcT.AddHours(1); // local changed after last sync
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        Assert.Single(fake.UpdatedLists);
        Assert.Equal("ext-1", fake.UpdatedLists[0].Id);
        Assert.Equal("New Name", fake.UpdatedLists[0].Dto.Name);
        Assert.Equal(1, session.Result.ExternalListsUpdated);
    }

    [Fact]
    public async Task Sync_WhenExternalModifiedAfterLastSync_UpdatesLocally()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "Updated Externally",
                    UpdatedAt = UtcT.AddHours(1), // external changed after last sync
                    Items = [],
                },
            ],
        };

        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "Old Local Name", ExternalId = "ext-1" };
                list.SyncedAt = UtcT;
                list.UpdatedAt = UtcT; // local not changed since last sync
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        var list = session.Db.TodoList.Single();

        Assert.Equal("Updated Externally", list.Name);
        Assert.Empty(fake.UpdatedLists);
        Assert.Equal(1, session.Result.LocalListsUpdated);
    }

    [Fact]
    public async Task Sync_WhenExternalListDeleted_DeletesLocally()
    {
        var fake = new FakeExternalTodoApiClient { ExternalLists = [] }; // external has nothing

        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "Will Be Deleted", ExternalId = "ext-deleted" };
                list.SyncedAt = DateTime.UtcNow;
                list.UpdatedAt = DateTime.UtcNow;
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        Assert.Empty(session.Db.TodoList.ToList());
        Assert.Equal(1, session.Result.LocalListsDeleted);
    }

    [Fact]
    public async Task Sync_WhenLocalListWithItemsHasNoExternalId_PushesItemsAndLinksIds()
    {
        var fake = new FakeExternalTodoApiClient();
        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "Shopping" };
                list.TodoItems.Add(new TodoItem { Description = "Buy milk" });
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        var list = session.Db.TodoList.Include(l => l.TodoItems).Single();
        var item = list.TodoItems.Single();

        Assert.NotNull(list.ExternalId);
        Assert.NotNull(item.ExternalId);
        Assert.Single(fake.CreatedLists[0].Items);
        Assert.Equal(1, session.Result.ExternalListsCreated);
    }

    [Fact]
    public async Task Sync_WhenExternalListHasItems_PullsItemsLocally()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "Shopping",
                    UpdatedAt = DateTime.UtcNow,
                    Items =
                    [
                        new ExternalTodoItemDto
                        {
                            Id = "item-ext-1",
                            Description = "Buy milk",
                            Completed = false,
                            UpdatedAt = DateTime.UtcNow,
                        },
                    ],
                },
            ],
        };

        await using var session = await RunSyncAsync(fakeClient: fake);

        var item = session.Db.TodoList.Include(l => l.TodoItems).Single().TodoItems.Single();

        Assert.Equal("Buy milk", item.Description);
        Assert.Equal("item-ext-1", item.ExternalId);
        Assert.Equal(1, session.Result.LocalItemsCreated);
    }

    [Fact]
    public async Task Sync_WhenExternalItemDeleted_DeletesItemLocally()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "Shopping",
                    UpdatedAt = UtcT,
                    Items = [], // item gone from external
                },
            ],
        };

        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "Shopping", ExternalId = "ext-1" };
                list.SyncedAt = UtcT;
                list.UpdatedAt = UtcT;
                var item = new TodoItem { Description = "Buy milk", ExternalId = "item-ext-1" };
                item.SyncedAt = UtcT;
                item.UpdatedAt = UtcT;
                list.TodoItems.Add(item);
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        Assert.Empty(session.Db.TodoItem.ToList());
        Assert.Equal(1, session.Result.LocalItemsDeleted);
    }

    [Fact]
    public async Task Sync_WhenLocalItemModifiedAfterLastSync_UpdatesExternal()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "Shopping",
                    UpdatedAt = UtcT,
                    Items =
                    [
                        new ExternalTodoItemDto
                        {
                            Id = "item-ext-1",
                            Description = "Buy milk",
                            Completed = false,
                            UpdatedAt = UtcT, // external not changed
                        },
                    ],
                },
            ],
        };

        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "Shopping", ExternalId = "ext-1" };
                list.SyncedAt = UtcT;
                list.UpdatedAt = UtcT;
                var item = new TodoItem
                {
                    Description = "Buy milk (organic)",
                    ExternalId = "item-ext-1",
                };
                item.SyncedAt = UtcT;
                item.UpdatedAt = UtcT.AddHours(1); // local changed after last sync
                list.TodoItems.Add(item);
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        Assert.Single(fake.UpdatedItems);
        Assert.Equal("item-ext-1", fake.UpdatedItems[0].ItemId);
        Assert.Equal("Buy milk (organic)", fake.UpdatedItems[0].Dto.Description);
        Assert.Equal(1, session.Result.ExternalItemsUpdated);
    }

    [Fact]
    public async Task Sync_WhenExternalItemModifiedAfterLastSync_UpdatesLocally()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "Shopping",
                    UpdatedAt = UtcT,
                    Items =
                    [
                        new ExternalTodoItemDto
                        {
                            Id = "item-ext-1",
                            Description = "Buy milk (organic)",
                            Completed = true,
                            UpdatedAt = UtcT.AddHours(1), // external changed after last sync
                        },
                    ],
                },
            ],
        };

        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "Shopping", ExternalId = "ext-1" };
                list.SyncedAt = UtcT;
                list.UpdatedAt = UtcT;
                var item = new TodoItem
                {
                    Description = "Buy milk",
                    IsCompleted = false,
                    ExternalId = "item-ext-1",
                };
                item.SyncedAt = UtcT;
                item.UpdatedAt = UtcT; // local not changed since last sync
                list.TodoItems.Add(item);
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        var item = session.Db.TodoItem.Single();

        Assert.Equal("Buy milk (organic)", item.Description);
        Assert.True(item.IsCompleted);
        Assert.Empty(fake.UpdatedItems);
        Assert.Equal(1, session.Result.LocalItemsUpdated);
    }

    [Fact]
    public async Task Sync_WhenRunTwiceOnSameDatabase_SecondRunMakesNoChanges()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "Stable List",
                    UpdatedAt = UtcT,
                    Items = [],
                },
            ],
        };

        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(fake, dbName);
        using (var seedScope = factory.Services.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<TodoContext>();
            ctx.Database.EnsureCreated();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var sync = scope.ServiceProvider.GetRequiredService<ISyncService>();
            var r1 = await sync.SyncAsync();
            Assert.Equal(1, r1.LocalListsCreated);

            var r2 = await sync.SyncAsync();
            Assert.Equal(0, r2.LocalListsCreated);
            Assert.Equal(0, r2.LocalListsUpdated);
            Assert.Equal(0, r2.ExternalListsCreated);
            Assert.Equal(0, r2.ExternalListsUpdated);

            var ctx = scope.ServiceProvider.GetRequiredService<TodoContext>();
            Assert.Single(ctx.TodoList.ToList());
        }
    }

    [Fact]
    public async Task Sync_WhenExternalListReferencesLocalSourceId_DoesNotCreateDuplicateList()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-canonical",
                    Name = "Mine",
                    UpdatedAt = UtcT,
                    Items = [],
                },
                new ExternalTodoListDto
                {
                    Id = "ext-other",
                    SourceId = "1", // matches seeded local list Id after first save
                    Name = "Echo",
                    UpdatedAt = UtcT,
                    Items = [],
                },
            ],
        };

        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "Mine", ExternalId = "ext-canonical" };
                list.SyncedAt = UtcT;
                list.UpdatedAt = UtcT;
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        Assert.Single(session.Db.TodoList);
        Assert.Equal(0, session.Result.LocalListsCreated);
    }

    [Fact]
    public async Task Sync_WhenExternalItemReferencesLocalSourceId_DoesNotCreateDuplicateItem()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "Shopping",
                    UpdatedAt = UtcT,
                    Items =
                    [
                        new ExternalTodoItemDto
                        {
                            Id = "item-canonical",
                            Description = "Eggs",
                            Completed = false,
                            UpdatedAt = UtcT,
                        },
                        new ExternalTodoItemDto
                        {
                            Id = "item-other",
                            SourceId = "1",
                            Description = "Echo row",
                            Completed = false,
                            UpdatedAt = UtcT,
                        },
                    ],
                },
            ],
        };

        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "Shopping", ExternalId = "ext-1" };
                list.SyncedAt = UtcT;
                list.UpdatedAt = UtcT;
                var item = new TodoItem { Description = "Eggs", ExternalId = "item-canonical" };
                item.SyncedAt = UtcT;
                item.UpdatedAt = UtcT;
                list.TodoItems.Add(item);
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        Assert.Single(session.Db.TodoItem);
        Assert.Equal(0, session.Result.LocalItemsCreated);
    }

    [Fact]
    public async Task Sync_WhenLocalItemHasNoExternalId_DoesNotUpdateExternal()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "With Local-Only Item",
                    UpdatedAt = UtcT,
                    Items = [],
                },
            ],
        };

        await using var session = await RunSyncAsync(
            seed: ctx =>
            {
                var list = new TodoList { Name = "With Local-Only Item", ExternalId = "ext-1" };
                list.SyncedAt = UtcT;
                list.UpdatedAt = UtcT;
                list.TodoItems.Add(new TodoItem { Description = "Cannot push yet" });
                ctx.TodoList.Add(list);
            },
            fakeClient: fake
        );

        var item = session.Db.TodoList.Include(l => l.TodoItems).Single().TodoItems.Single();
        Assert.Null(item.ExternalId);
        Assert.Empty(fake.UpdatedItems);
        Assert.Equal(0, session.Result.ExternalItemsUpdated);
    }
}
