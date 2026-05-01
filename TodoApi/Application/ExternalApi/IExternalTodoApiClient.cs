using TodoApi.Application.ExternalApi.Dtos;

namespace TodoApi.Application.ExternalApi;

public interface IExternalTodoApiClient
{
    Task<IReadOnlyList<ExternalTodoListDto>> GetAllAsync(CancellationToken ct = default);

    Task<ExternalTodoListDto> CreateTodoListAsync(CreateExternalTodoListDto dto, CancellationToken ct = default);

    Task<ExternalTodoListDto> UpdateTodoListAsync(string externalId, UpdateExternalTodoListDto dto, CancellationToken ct = default);

    Task DeleteTodoListAsync(string externalId, CancellationToken ct = default);

    Task<ExternalTodoItemDto> UpdateTodoItemAsync(string listExternalId, string itemExternalId, UpdateExternalTodoItemDto dto, CancellationToken ct = default);

    Task DeleteTodoItemAsync(string listExternalId, string itemExternalId, CancellationToken ct = default);
}
