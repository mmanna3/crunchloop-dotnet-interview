using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Models;
using TodoApi.Domain.Repositories;

namespace TodoApi.Persistence;

public class TodoListsRepository(TodoContext context) : ITodoListsRepository
{
    private readonly TodoContext _context = context;

    public async Task<IReadOnlyList<TodoList>> GetTodoLists()
    {
        return await _context.TodoList.ToListAsync();
    }

    public async Task<TodoList?> GetTodoList(long id)
    {
        return await _context.TodoList.FindAsync(id);
    }

    public async Task<TodoList> CreateTodoList(TodoList payload)
    {
        _context.TodoList.Add(payload);
        await _context.SaveChangesAsync();
        return payload;
    }

    public async Task<TodoList?> UpdateTodoList(long id, TodoList payload)
    {
        var todoList = await _context.TodoList.FindAsync(id);
        if (todoList == null)
        {
            return null;
        }

        todoList.Name = payload.Name;
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
