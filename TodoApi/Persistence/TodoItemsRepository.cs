using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Models;
using TodoApi.Domain.Repositories;

namespace TodoApi.Persistence;

public class TodoItemsRepository(TodoContext context) : ITodoItemsRepository
{
    private readonly TodoContext _context = context;

    public async Task<IReadOnlyList<TodoItem>> GetTodoItems(long listId)
    {
        return await _context.TodoItem.Where(item => item.TodoListId == listId).ToListAsync();
    }

    public async Task<TodoItem?> GetTodoItem(long id)
    {
        return await _context.TodoItem.SingleOrDefaultAsync(item => item.Id == id);
    }

    public async Task<TodoItem> CreateTodoItem(TodoItem payload)
    {
        _context.TodoItem.Add(payload);
        await _context.SaveChangesAsync();
        return payload;
    }

    public async Task<TodoItem> UpdateTodoItem(TodoItem payload)
    {
        await _context.SaveChangesAsync();
        return payload;
    }

    public async Task DeleteTodoItem(TodoItem payload)
    {
        _context.TodoItem.Remove(payload);
        await _context.SaveChangesAsync();
    }
}
