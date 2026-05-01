using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.Application.Dtos;
using TodoApi.Domain.Models;
using TodoApi.Persistence;

namespace TodoApi.Tests.IntegrationTests;

public class TodoListsIT
{
    private static HttpClient CreateClientWithDatabaseSeed(Action<TodoContext>? seed = null)
    {
        var databaseName = Guid.NewGuid().ToString();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<TodoContext>));
                services.RemoveAll<TodoContext>();
                services.AddDbContext<TodoContext>(options =>
                    options.UseInMemoryDatabase(databaseName)
                );

                using var scope = services.BuildServiceProvider().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
                context.Database.EnsureCreated();
                seed?.Invoke(context);
                context.SaveChanges();
            });
        });

        return factory.CreateClient();
    }

    private static void PopulateDatabaseContext(TodoContext context)
    {
        context.TodoList.Add(new TodoList { Id = 1, Name = "Task List 1" });
        context.TodoList.Add(new TodoList { Id = 2, Name = "Task List 2" });
    }

    private static void PopulateDatabaseContextWithIncompleteItems(TodoContext context)
    {
        PopulateDatabaseContext(context);
        context.TodoItem.Add(
            new TodoItem
            {
                Id = 1,
                Description = "Incomplete task",
                TodoListId = 1,
                IsCompleted = false,
            }
        );
        context.TodoItem.Add(
            new TodoItem
            {
                Id = 2,
                Description = "Complete task",
                TodoListId = 1,
                IsCompleted = true,
            }
        );
        context.TodoItem.Add(
            new TodoItem
            {
                Id = 3,
                Description = "Another incomplete",
                TodoListId = 1,
                IsCompleted = false,
            }
        );
    }

    [Fact]
    public async Task GetTodoList_WhenCalled_ReturnsTodoListList()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.GetAsync("/api/todolists");
        var todoLists = await response.Content.ReadFromJsonAsync<List<TodoListDTO>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(todoLists);
        Assert.Equal(2, todoLists.Count);
    }

    [Fact]
    public async Task GetTodoList_WhenCalled_ReturnsTodoListById()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.GetAsync("/api/todolists/1");
        var todoList = await response.Content.ReadFromJsonAsync<TodoListDTO>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(todoList);
        Assert.Equal(1, todoList.Id);
    }

    [Fact]
    public async Task GetTodoList_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.GetAsync("/api/todolists/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutTodoList_WhenCalled_UpdatesTheTodoList()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.PutAsJsonAsync(
            "/api/todolists/2",
            new UpdateListDTO { Name = "Changed Task List 2" }
        );
        var todoList = await response.Content.ReadFromJsonAsync<TodoListDTO>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(todoList);
        Assert.Equal("Changed Task List 2", todoList.Name);
    }

    [Fact]
    public async Task PutTodoList_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.PutAsJsonAsync(
            "/api/todolists/999",
            new UpdateListDTO { Name = "Updated Task List" }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostTodoList_WhenCalled_CreatesTodoList()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.PostAsJsonAsync(
            "/api/todolists",
            new CreateListDTO { Name = "Task List 3" }
        );
        var todoList = await response.Content.ReadFromJsonAsync<TodoListDTO>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(todoList);
        Assert.Equal("Task List 3", todoList.Name);
    }

    [Fact]
    public async Task DeleteTodoList_WhenCalled_RemovesTodoList()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.DeleteAsync("/api/todolists/2");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodoList_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.DeleteAsync("/api/todolists/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteAllItems_WhenListExists_ReturnsAcceptedWithTotals()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContextWithIncompleteItems);

        var response = await client.PostAsJsonAsync(
            "/api/todolists/1/complete-all-items",
            new CompleteAllItemsRequestDTO { ConnectionId = "test-signalr-connection" }
        );
        var body = await response.Content.ReadFromJsonAsync<CompleteAllItemsResponseDTO>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(Guid.TryParse(body.OperationId, out _), body.OperationId);
        Assert.Equal(2, body.Total);
    }
}
