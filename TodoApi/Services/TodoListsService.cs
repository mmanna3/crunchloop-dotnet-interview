using TodoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace TodoApi.Services
{
    public class TodoListService(TodoContext context) : ITodoListService
    {
        private readonly TodoContext _context = context;

        public async Task<IList<TodoList>> GetTodoLists()
        {
            return await _context.TodoList.ToListAsync();
        }

        public async Task<TodoList?> GetTodoList(long id)
        {
            return await _context.TodoList.FindAsync(id);
        }

        public async Task<TodoList> CreateTodoList(string name)
        {
            var todoList = new TodoList { Name = name };
            _context.TodoList.Add(todoList);
            await _context.SaveChangesAsync();
            return todoList;
        }

        public async Task<TodoList?> UpdateTodoList(long id, string name)
        {
            var todoList = await _context.TodoList.FindAsync(id);
            if (todoList == null)
            {
                return null;
            }

            todoList.Name = name;
            await _context.SaveChangesAsync();
            return todoList;
        }

        public async Task<bool> DeleteTodoList(long id)
        {
            var todoList = await _context.TodoList.FindAsync(id);
            if (todoList == null)
            {
                return false;
            }

            _context.TodoList.Remove(todoList);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}