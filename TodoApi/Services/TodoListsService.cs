using TodoApi.Models;
using TodoApi.Persistence;

namespace TodoApi.Services
{
    public class TodoListService(ITodoListsRepository todoListsRepository) : ITodoListService
    {
        private readonly ITodoListsRepository _todoListsRepository = todoListsRepository;

        public async Task<IList<TodoList>> GetTodoLists()
        {
            return await _todoListsRepository.GetTodoLists();
        }

        public async Task<TodoList?> GetTodoList(long id)
        {
            return await _todoListsRepository.GetTodoList(id);
        }
        public async Task<TodoList> CreateTodoList(string name)
        {
            return await _todoListsRepository.CreateTodoList(name);
        }

        public async Task<TodoList?> UpdateTodoList(long id, string name)
        {
            return await _todoListsRepository.UpdateTodoList(id, name);
        }

        public async Task<bool> DeleteTodoList(long id)
        {
            return await _todoListsRepository.DeleteTodoList(id);
        }
    }
}