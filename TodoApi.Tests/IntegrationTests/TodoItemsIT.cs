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

public class TodoItemsIT
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
        context.TodoItem.Add(
            new TodoItem
            {
                Id = 1,
                Description = "Item 1",
                TodoListId = 1,
            }
        );
        context.TodoItem.Add(
            new TodoItem
            {
                Id = 2,
                Description = "Item 2",
                TodoListId = 1,
            }
        );
        context.TodoItem.Add(
            new TodoItem
            {
                Id = 3,
                Description = "Item on list 2",
                TodoListId = 2,
            }
        );
    }

    [Fact]
    public async Task GetTodoItems_WhenCalled_ReturnsTodoItemsForList()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.GetAsync("/api/todolists/1/items");
        var items = await response.Content.ReadFromJsonAsync<List<TodoItem>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal(1L, i.TodoListId));
    }

    [Fact]
    public async Task GetTodoItems_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.GetAsync("/api/todolists/999/items");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTodoItem_WhenCalled_ReturnsTodoItemById()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.GetAsync("/api/todolists/1/items/1");
        var item = await response.Content.ReadFromJsonAsync<TodoItem>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(item);
        Assert.Equal(1, item.Id);
        Assert.Equal("Item 1", item.Description);
    }

    [Fact]
    public async Task GetTodoItem_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.GetAsync("/api/todolists/999/items/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTodoItem_WhenTodoItemDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.GetAsync("/api/todolists/1/items/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostTodoItem_WhenCalled_CreatesTodoItem()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.PostAsJsonAsync(
            "/api/todolists/1/items",
            new CreateItemDTO { Description = "New item" }
        );
        var item = await response.Content.ReadFromJsonAsync<TodoItem>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(item);
        Assert.Equal("New item", item.Description);
        Assert.Equal(1L, item.TodoListId);
    }

    [Fact]
    public async Task PostTodoItem_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.PostAsJsonAsync(
            "/api/todolists/999/items",
            new CreateItemDTO { Description = "Orphan item" }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Todo list not found", body);
    }

    [Fact]
    public async Task PutTodoItem_WhenCalled_UpdatesTheTodoItem()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.PutAsJsonAsync(
            "/api/todolists/1/items/1",
            new UpdateItemDTO { Description = "Updated item 1", IsCompleted = true }
        );
        var item = await response.Content.ReadFromJsonAsync<TodoItem>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(item);
        Assert.Equal("Updated item 1", item.Description);
        Assert.True(item.IsCompleted);
    }

    [Fact]
    public async Task PutTodoItem_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.PutAsJsonAsync(
            "/api/todolists/999/items/1",
            new UpdateItemDTO { Description = "X", IsCompleted = false }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Todo list not found", body);
    }

    [Fact]
    public async Task PutTodoItem_WhenTodoItemDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.PutAsJsonAsync(
            "/api/todolists/1/items/999",
            new UpdateItemDTO { Description = "X", IsCompleted = false }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Todo item not found", body);
    }

    [Fact]
    public async Task DeleteTodoItem_WhenCalled_RemovesTodoItem()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.DeleteAsync("/api/todolists/1/items/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodoItem_WhenTodoListDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.DeleteAsync("/api/todolists/999/items/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Todo list not found", body);
    }

    [Fact]
    public async Task DeleteTodoItem_WhenTodoItemDoesntExist_ReturnsNotFound()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.DeleteAsync("/api/todolists/1/items/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Todo item not found", body);
    }

    [Fact]
    public async Task DeleteTodoItem_WhenTodoItemBelongsToAnotherList_ReturnsBadRequest()
    {
        using var client = CreateClientWithDatabaseSeed(PopulateDatabaseContext);

        var response = await client.DeleteAsync("/api/todolists/1/items/3");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Todo item does not belong to this list", body);
    }
}
