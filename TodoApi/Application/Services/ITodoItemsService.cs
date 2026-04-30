using TodoApi.Application.Dtos;

namespace TodoApi.Application.Services;

public interface ITodoItemsService
{
    Task<IReadOnlyList<TodoItemDTO>> GetTodoItems(long listId);
    Task<TodoItemDTO> GetTodoItem(long listId, long id);
    Task<TodoItemDTO> CreateTodoItem(long listId, CreateItemDTO payload);
    Task<TodoItemDTO> UpdateTodoItem(long listId, long id, UpdateItemDTO payload);
    Task<bool> DeleteTodoItem(long listId, long id);
}
