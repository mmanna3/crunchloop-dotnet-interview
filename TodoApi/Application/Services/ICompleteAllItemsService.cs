using TodoApi.Application.Dtos;

namespace TodoApi.Application.Services;

public interface ICompleteAllItemsService
{
    Task<CompleteAllItemsResponseDTO> StartAsync(long listId, string connectionId);
}
