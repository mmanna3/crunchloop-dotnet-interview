using TodoApi.Domain.Models;

namespace TodoApi.Domain.Repositories;

public interface ITodoItemsRepository
{
    Task<IList<TodoItem>> GetTodoItems(long listId);
    Task<TodoItem?> GetTodoItem(long listId, long id);
    Task<TodoItem> CreateTodoItem(long listId, string description);
    Task<TodoItem?> UpdateTodoItem(long listId, long id, string description);
    Task<bool> DeleteTodoItem(long listId, long id);
}
