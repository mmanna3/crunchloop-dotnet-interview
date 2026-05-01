using TodoApi.Application.Dtos;
using TodoApi.Application.Exceptions;
using TodoApi.Domain.Models;
using TodoApi.Domain.Repositories;

namespace TodoApi.Application.Services;

public class TodoItemsService(
    ITodoItemsRepository todoItemsRepository,
    ITodoListsRepository todoListsRepository
) : ITodoItemsService
{
    private static class ExceptionMessages
    {
        public const string TodoListNotFound = "Todo list not found";
        public const string TodoItemNotFound = "Todo item not found";
        public const string TodoItemDoesNotBelongToList = "Todo item does not belong to this list";
    }

    private readonly ITodoItemsRepository _todoItemsRepository = todoItemsRepository;
    private readonly ITodoListsRepository _todoListsRepository = todoListsRepository;

    public async Task<IReadOnlyList<TodoItemDTO>> GetTodoItems(long listId)
    {
        if (!await _todoListsRepository.TodoListExists(listId))
        {
            throw new NotFoundException(ExceptionMessages.TodoListNotFound);
        }

        var items = await _todoItemsRepository.GetTodoItems(listId);
        return items.Select(ToItemDto).ToList();
    }

    public async Task<TodoItemDTO> GetTodoItem(long listId, long id)
    {
        var item = await EnsureTodoItemBelongsToListOrThrowAsync(listId, id);
        return ToItemDto(item);
    }

    public async Task<TodoItemDTO> CreateTodoItem(long listId, CreateItemDTO payload)
    {
        if (!await _todoListsRepository.TodoListExists(listId))
        {
            throw new NotFoundException(ExceptionMessages.TodoListNotFound);
        }

        var created = await _todoItemsRepository.CreateTodoItem(
            new TodoItem { Description = payload.Description, TodoListId = listId }
        );
        return ToItemDto(created);
    }

    public async Task<TodoItemDTO> UpdateTodoItem(long listId, long id, UpdateItemDTO payload)
    {
        var item = await EnsureTodoItemBelongsToListOrThrowAsync(listId, id);
        item.Description = payload.Description;
        item.IsCompleted = payload.IsCompleted;
        var updated = await _todoItemsRepository.UpdateTodoItem(item);
        return ToItemDto(updated);
    }

    public async Task<bool> DeleteTodoItem(long listId, long id)
    {
        var item = await EnsureTodoItemBelongsToListOrThrowAsync(listId, id);
        await _todoItemsRepository.DeleteTodoItem(item);
        return true;
    }

    private async Task<TodoItem> EnsureTodoItemBelongsToListOrThrowAsync(long listId, long id)
    {
        if (!await _todoListsRepository.TodoListExists(listId))
        {
            throw new NotFoundException(ExceptionMessages.TodoListNotFound);
        }

        var item = await _todoItemsRepository.GetTodoItem(id);
        if (item == null)
        {
            throw new NotFoundException(ExceptionMessages.TodoItemNotFound);
        }

        if (item.TodoListId != listId)
        {
            throw new ValidationException(ExceptionMessages.TodoItemDoesNotBelongToList);
        }

        return item;
    }

    private static TodoItemDTO ToItemDto(TodoItem item)
    {
        return new TodoItemDTO
        {
            Id = item.Id,
            Description = item.Description,
            IsCompleted = item.IsCompleted,
        };
    }
}
