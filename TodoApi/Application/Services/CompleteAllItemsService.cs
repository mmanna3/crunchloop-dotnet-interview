using System.Threading.Channels;
using TodoApi.Application.Dtos;
using TodoApi.Application.Exceptions;
using TodoApi.Application.Workers;
using TodoApi.Domain.Repositories;

namespace TodoApi.Application.Services;

public class CompleteAllItemsService(
    ITodoListsRepository todoListsRepository,
    ITodoItemsRepository todoItemsRepository,
    Channel<CompleteAllItemsJob> channel
) : ICompleteAllItemsService
{
    public async Task<CompleteAllItemsResponseDTO> StartAsync(long listId, string connectionId)
    {
        if (!await todoListsRepository.TodoListExists(listId))
            throw new NotFoundException("Todo list not found");

        var total = await todoItemsRepository.CountIncompleteByListIdAsync(listId);
        var operationId = Guid.NewGuid();

        await channel.Writer.WriteAsync(
            new CompleteAllItemsJob
            {
                OperationId = operationId,
                ListId = listId,
                Total = total,
                ConnectionId = connectionId,
            }
        );

        return new CompleteAllItemsResponseDTO
        {
            OperationId = operationId.ToString(),
            Total = total,
        };
    }
}
