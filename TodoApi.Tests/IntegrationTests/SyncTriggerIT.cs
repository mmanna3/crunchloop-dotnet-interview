using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.Application.Dtos;
using TodoApi.Application.ExternalApi;
using TodoApi.Application.ExternalApi.Dtos;
using TodoApi.Persistence;
using TodoApi.Tests.Helpers;

namespace TodoApi.Tests.IntegrationTests;

// See SyncServiceIT for why this collection exists.
[Collection("Sync")]
public class SyncTriggerIT
{
    private static HttpClient CreateClient(FakeExternalTodoApiClient? fakeClient = null)
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<TodoContext>));
                services.AddDbContext<TodoContext>(o => o.UseInMemoryDatabase(dbName));
                services.RemoveAll<IExternalTodoApiClient>();
                services.AddSingleton<IExternalTodoApiClient>(
                    fakeClient ?? new FakeExternalTodoApiClient()
                );
            });
        });

        return factory.CreateClient();
    }

    [Fact]
    public async Task PostTrigger_Returns200_WithSyncResult()
    {
        using var client = CreateClient();

        var response = await client.PostAsync("/api/sync/trigger", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SyncTriggerResponseDTO>();
        Assert.NotNull(body);
        Assert.False(body.WasSkipped);
    }

    [Fact]
    public async Task PostTrigger_WhenExternalHasNewList_ReturnsCorrectCounters()
    {
        var fake = new FakeExternalTodoApiClient
        {
            ExternalLists =
            [
                new ExternalTodoListDto
                {
                    Id = "ext-1",
                    Name = "From External",
                    UpdatedAt = DateTime.UtcNow,
                    Items = [],
                },
            ],
        };
        using var client = CreateClient(fake);

        var response = await client.PostAsync("/api/sync/trigger", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SyncTriggerResponseDTO>();
        Assert.NotNull(body);
        Assert.Equal(1, body.LocalListsCreated);
        Assert.Equal(0, body.ExternalListsCreated);
    }
}
