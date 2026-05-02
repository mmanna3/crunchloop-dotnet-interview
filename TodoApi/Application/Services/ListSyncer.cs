using TodoApi.Application.ExternalApi;
using TodoApi.Application.ExternalApi.Dtos;
using TodoApi.Domain.Models;
using TodoApi.Persistence;

namespace TodoApi.Application.Services;

public class ListSyncer(IExternalTodoApiClient externalClient, TodoContext context, ILogger logger)
{
    public async Task ReconcileAsync(
        List<TodoList> localLists,
        IReadOnlyList<ExternalTodoListDto> externalLists,
        SyncResult result,
        CancellationToken ct
    )
    {
        var externalById = externalLists.ToDictionary(x => x.Id);

        foreach (var local in localLists)
            await ProcessLocalListAsync(local, externalById, result, ct);

        var knownExternalIds = localLists
            .Where(l => l.ExternalId != null)
            .Select(l => l.ExternalId!)
            .ToHashSet();
        var knownSourceIds = localLists.Select(l => l.Id.ToString()).ToHashSet();

        foreach (var external in externalLists)
            if (
                !knownExternalIds.Contains(external.Id)
                && (external.SourceId == null || !knownSourceIds.Contains(external.SourceId))
            )
                CreateLocalListFromExternal(external, result);
    }

    private async Task ProcessLocalListAsync(
        TodoList local,
        Dictionary<string, ExternalTodoListDto> externalById,
        SyncResult result,
        CancellationToken ct
    )
    {
        if (local.ExternalId == null)
        {
            await PushNewListToExternalAsync(local, result, ct);
            return;
        }

        if (!externalById.TryGetValue(local.ExternalId, out var external))
        {
            context.TodoList.Remove(local);
            result.LocalListsDeleted++;
            return;
        }

        var (push, pull) = SyncDecision.Resolve(
            local.UpdatedAt,
            local.SyncedAt,
            external.UpdatedAt
        );

        if (push)
        {
            await externalClient.UpdateTodoListAsync(
                local.ExternalId,
                new UpdateExternalTodoListDto { Name = local.Name },
                ct
            );
            result.ExternalListsUpdated++;
        }
        else if (pull)
        {
            local.Name = external.Name;
            result.LocalListsUpdated++;
        }

        await ProcessItemsAsync(local, external, result, ct);
    }

    private async Task PushNewListToExternalAsync(
        TodoList local,
        SyncResult result,
        CancellationToken ct
    )
    {
        var created = await externalClient.CreateTodoListAsync(
            new CreateExternalTodoListDto
            {
                SourceId = local.Id.ToString(),
                Name = local.Name,
                Items = local
                    .TodoItems.Select(i => new CreateExternalTodoItemDto
                    {
                        SourceId = i.Id.ToString(),
                        Description = i.Description,
                        Completed = i.IsCompleted,
                    })
                    .ToList(),
            },
            ct
        );

        local.ExternalId = created.Id;

        var localItemBySourceId = local.TodoItems.ToDictionary(i => i.Id.ToString());
        foreach (var externalItem in created.Items.Where(i => i.SourceId != null))
            if (localItemBySourceId.TryGetValue(externalItem.SourceId!, out var localItem))
                localItem.ExternalId = externalItem.Id;

        result.ExternalListsCreated++;
    }

    private async Task ProcessItemsAsync(
        TodoList local,
        ExternalTodoListDto external,
        SyncResult result,
        CancellationToken ct
    )
    {
        var externalItemById = external.Items.ToDictionary(i => i.Id);
        var localItems = local.TodoItems.ToList();

        foreach (var localItem in localItems)
        {
            if (localItem.ExternalId == null)
            {
                logger.LogWarning(
                    "TodoItem {ItemId} in list {ListId} cannot be pushed: "
                        + "the external API has no endpoint to add items to an existing list",
                    localItem.Id,
                    local.Id
                );
                continue;
            }

            if (!externalItemById.TryGetValue(localItem.ExternalId, out var externalItem))
            {
                context.TodoItem.Remove(localItem);
                result.LocalItemsDeleted++;
                continue;
            }

            var (push, pull) = SyncDecision.Resolve(
                localItem.UpdatedAt,
                localItem.SyncedAt,
                externalItem.UpdatedAt
            );

            if (push)
            {
                await externalClient.UpdateTodoItemAsync(
                    local.ExternalId!,
                    localItem.ExternalId,
                    new UpdateExternalTodoItemDto
                    {
                        Description = localItem.Description,
                        Completed = localItem.IsCompleted,
                    },
                    ct
                );
                result.ExternalItemsUpdated++;
            }
            else if (pull)
            {
                localItem.Description = externalItem.Description;
                localItem.IsCompleted = externalItem.Completed;
                result.LocalItemsUpdated++;
            }
        }

        var localExternalItemIds = localItems
            .Where(i => i.ExternalId != null)
            .Select(i => i.ExternalId!)
            .ToHashSet();
        var localSourceItemIds = localItems.Select(i => i.Id.ToString()).ToHashSet();

        foreach (var externalItem in external.Items)
            if (
                !localExternalItemIds.Contains(externalItem.Id)
                && (
                    externalItem.SourceId == null
                    || !localSourceItemIds.Contains(externalItem.SourceId)
                )
            )
            {
                local.TodoItems.Add(ToLocalItem(externalItem));
                result.LocalItemsCreated++;
            }
    }

    private void CreateLocalListFromExternal(ExternalTodoListDto external, SyncResult result)
    {
        var newList = new TodoList { Name = external.Name, ExternalId = external.Id };

        foreach (var externalItem in external.Items)
        {
            newList.TodoItems.Add(ToLocalItem(externalItem));
            result.LocalItemsCreated++;
        }

        context.TodoList.Add(newList);
        result.LocalListsCreated++;
    }

    private static TodoItem ToLocalItem(ExternalTodoItemDto dto) =>
        new()
        {
            Description = dto.Description,
            IsCompleted = dto.Completed,
            ExternalId = dto.Id,
        };
}
