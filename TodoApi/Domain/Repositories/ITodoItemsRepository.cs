using TodoApi.Domain.Models;

namespace TodoApi.Domain.Repositories;

public interface ITodoItemsRepository
{
    Task<IReadOnlyList<TodoItem>> GetTodoItems(long listId);
    Task<TodoItem?> GetTodoItem(long id);
    Task<TodoItem> CreateTodoItem(TodoItem payload);
    Task<TodoItem> UpdateTodoItem(TodoItem payload);
    Task DeleteTodoItem(TodoItem payload);
}
