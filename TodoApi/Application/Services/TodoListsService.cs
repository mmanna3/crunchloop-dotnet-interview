using TodoApi.Application.Dtos;
using TodoApi.Domain.Models;
using TodoApi.Domain.Repositories;

namespace TodoApi.Application.Services;

public class TodoListService(ITodoListsRepository todoListsRepository) : ITodoListService
{
    private readonly ITodoListsRepository _todoListsRepository = todoListsRepository;

    public async Task<IReadOnlyList<TodoListDTO>> GetTodoLists()
    {
        var lists = await _todoListsRepository.GetTodoLists();
        var result = lists
            .Select(list => new TodoListDTO { Id = list.Id, Name = list.Name })
            .ToList();
        return result;
    }

    public async Task<TodoListDTO?> GetTodoList(long id)
    {
        var list = await _todoListsRepository.GetTodoList(id);
        if (list == null)
        {
            return null;
        }
        return new TodoListDTO { Id = list.Id, Name = list.Name };
    }

    public async Task<TodoListDTO> CreateTodoList(CreateListDTO payload)
    {
        var list = await _todoListsRepository.CreateTodoList(new TodoList { Name = payload.Name });
        return new TodoListDTO { Id = list.Id, Name = list.Name };
    }

    public async Task<TodoListDTO?> UpdateTodoList(long id, UpdateListDTO payload)
    {
        var updatedTodoList = await _todoListsRepository.UpdateTodoList(
            id,
            new TodoList { Name = payload.Name }
        );
        if (updatedTodoList == null)
        {
            return null;
        }
        return new TodoListDTO { Id = updatedTodoList.Id, Name = updatedTodoList.Name };
    }

    public async Task<bool> DeleteTodoList(long id)
    {
        return await _todoListsRepository.DeleteTodoList(id);
    }
}
