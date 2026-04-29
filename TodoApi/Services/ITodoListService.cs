using TodoApi.Models;

namespace TodoApi.Services
{
    public interface ITodoListService
    {
        Task<IList<TodoList>> GetTodoLists();
        Task<TodoList?> GetTodoList(long id);
        Task<TodoList> CreateTodoList(string name);        
        Task<TodoList?> UpdateTodoList(long id, string name);        
        Task<bool> DeleteTodoList(long id);
    }
}