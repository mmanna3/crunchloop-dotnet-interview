using TodoApi.Application.ExternalApi;
using TodoApi.Application.ExternalApi.Dtos;

namespace TodoApi.Tests.Helpers;

internal class FakeExternalTodoApiClient : IExternalTodoApiClient
{
    public List<ExternalTodoListDto> ExternalLists { get; set; } = [];
    public List<CreateExternalTodoListDto> CreatedLists { get; } = [];
    public List<(string Id, UpdateExternalTodoListDto Dto)> UpdatedLists { get; } = [];
    public List<string> DeletedListIds { get; } = [];
    public List<(
        string ListId,
        string ItemId,
        UpdateExternalTodoItemDto Dto
    )> UpdatedItems { get; } = [];

    public Task<IReadOnlyList<ExternalTodoListDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ExternalTodoListDto>>(ExternalLists);

    public Task<ExternalTodoListDto> CreateTodoListAsync(
        CreateExternalTodoListDto dto,
        CancellationToken ct = default
    )
    {
        CreatedLists.Add(dto);
        var created = new ExternalTodoListDto
        {
            Id = Guid.NewGuid().ToString(),
            SourceId = dto.SourceId,
            Name = dto.Name,
            UpdatedAt = DateTime.UtcNow,
            Items = dto
                .Items.Select(i => new ExternalTodoItemDto
                {
                    Id = Guid.NewGuid().ToString(),
                    SourceId = i.SourceId,
                    Description = i.Description,
                    Completed = i.Completed,
                    UpdatedAt = DateTime.UtcNow,
                })
                .ToList(),
        };
        ExternalLists.Add(created);
        return Task.FromResult(created);
    }

    public Task<ExternalTodoListDto> UpdateTodoListAsync(
        string externalId,
        UpdateExternalTodoListDto dto,
        CancellationToken ct = default
    )
    {
        UpdatedLists.Add((externalId, dto));
        var list = ExternalLists.FirstOrDefault(l => l.Id == externalId);
        if (list != null)
            list.Name = dto.Name;
        return Task.FromResult(
            list ?? new ExternalTodoListDto { Id = externalId, Name = dto.Name }
        );
    }

    public Task DeleteTodoListAsync(string externalId, CancellationToken ct = default)
    {
        DeletedListIds.Add(externalId);
        return Task.CompletedTask;
    }

    public Task<ExternalTodoItemDto> UpdateTodoItemAsync(
        string listExternalId,
        string itemExternalId,
        UpdateExternalTodoItemDto dto,
        CancellationToken ct = default
    )
    {
        UpdatedItems.Add((listExternalId, itemExternalId, dto));
        return Task.FromResult(
            new ExternalTodoItemDto
            {
                Id = itemExternalId,
                Description = dto.Description,
                Completed = dto.Completed,
                UpdatedAt = DateTime.UtcNow,
            }
        );
    }

    public Task DeleteTodoItemAsync(
        string listExternalId,
        string itemExternalId,
        CancellationToken ct = default
    ) => Task.CompletedTask;
}
