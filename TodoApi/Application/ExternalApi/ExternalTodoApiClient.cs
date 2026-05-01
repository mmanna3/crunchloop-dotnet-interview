using System.Net;
using System.Net.Http.Json;
using TodoApi.Application.ExternalApi.Dtos;

namespace TodoApi.Application.ExternalApi;

public class ExternalTodoApiClient(HttpClient httpClient, ILogger<ExternalTodoApiClient> logger)
    : IExternalTodoApiClient
{
    public async Task<IReadOnlyList<ExternalTodoListDto>> GetAllAsync(
        CancellationToken ct = default
    )
    {
        var response = await httpClient.GetAsync("todolists", ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ExternalTodoListDto>>(ct) ?? [];
    }

    public async Task<ExternalTodoListDto> CreateTodoListAsync(
        CreateExternalTodoListDto dto,
        CancellationToken ct = default
    )
    {
        var response = await httpClient.PostAsJsonAsync("todolists", dto, ct);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExternalTodoListDto>(ct))!;
    }

    public async Task<ExternalTodoListDto> UpdateTodoListAsync(
        string externalId,
        UpdateExternalTodoListDto dto,
        CancellationToken ct = default
    )
    {
        var response = await httpClient.PatchAsJsonAsync($"todolists/{externalId}", dto, ct);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExternalTodoListDto>(ct))!;
    }

    public async Task DeleteTodoListAsync(string externalId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"todolists/{externalId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning(
                "TodoList {ExternalId} not found in external API; treating as already deleted",
                externalId
            );
            return;
        }
        await EnsureSuccessAsync(response);
    }

    public async Task<ExternalTodoItemDto> UpdateTodoItemAsync(
        string listExternalId,
        string itemExternalId,
        UpdateExternalTodoItemDto dto,
        CancellationToken ct = default
    )
    {
        var response = await httpClient.PatchAsJsonAsync(
            $"todolists/{listExternalId}/todoitems/{itemExternalId}",
            dto,
            ct
        );
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ExternalTodoItemDto>(ct))!;
    }

    public async Task DeleteTodoItemAsync(
        string listExternalId,
        string itemExternalId,
        CancellationToken ct = default
    )
    {
        var response = await httpClient.DeleteAsync(
            $"todolists/{listExternalId}/todoitems/{itemExternalId}",
            ct
        );
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning(
                "TodoItem {ItemExternalId} in list {ListExternalId} not found in external API; treating as already deleted",
                itemExternalId,
                listExternalId
            );
            return;
        }
        await EnsureSuccessAsync(response);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        logger.LogError(
            "External API error {StatusCode} on {Method} {Url}: {Body}",
            (int)response.StatusCode,
            response.RequestMessage?.Method,
            response.RequestMessage?.RequestUri,
            body
        );

        response.EnsureSuccessStatusCode();
    }
}
