using TodoApi.Application.Dtos;
using TodoApi.Domain.Models;

namespace TodoApi.Application.Services;

public interface ITodoListService
{
    Task<IReadOnlyList<TodoListDTO>> GetTodoLists();
    Task<TodoListDTO?> GetTodoList(long id);
    Task<TodoListDTO> CreateTodoList(CreateListDTO payload);
    Task<TodoListDTO?> UpdateTodoList(long id, UpdateListDTO payload);
    Task<bool> DeleteTodoList(long id);
}
