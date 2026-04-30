using TodoApi.Domain.Models;

namespace TodoApi.Domain.Repositories;

public interface ITodoListsRepository
{
    Task<IList<TodoList>> GetTodoLists();
    Task<TodoList?> GetTodoList(long id);
    Task<TodoList> CreateTodoList(string name);
    Task<TodoList?> UpdateTodoList(long id, string name);
    Task<bool> DeleteTodoList(long id);
}
