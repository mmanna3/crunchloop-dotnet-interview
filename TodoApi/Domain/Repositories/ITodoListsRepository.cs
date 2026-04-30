using TodoApi.Domain.Models;

namespace TodoApi.Domain.Repositories;

public interface ITodoListsRepository
{
    Task<IReadOnlyList<TodoList>> GetTodoLists();
    Task<TodoList?> GetTodoList(long id);
    Task<TodoList> CreateTodoList(TodoList payload);
    Task<TodoList?> UpdateTodoList(long id, TodoList payload);
    Task<bool> DeleteTodoList(long id);
}
