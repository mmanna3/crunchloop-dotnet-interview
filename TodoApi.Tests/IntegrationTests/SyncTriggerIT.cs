using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.Application.Dtos;
using TodoApi.Application.ExternalApi;
using TodoApi.Persistence;
using TodoApi.Tests.Helpers;

namespace TodoApi.Tests.IntegrationTests;

public class SyncTriggerIT
{
    private static HttpClient CreateClient()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<TodoContext>));
                services.AddDbContext<TodoContext>(o => o.UseInMemoryDatabase(dbName));
                services.RemoveAll<IExternalTodoApiClient>();
                services.AddSingleton<IExternalTodoApiClient>(new FakeExternalTodoApiClient());
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
}
