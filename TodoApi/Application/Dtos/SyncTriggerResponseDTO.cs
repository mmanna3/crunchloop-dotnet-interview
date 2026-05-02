using TodoApi.Application.Services;

namespace TodoApi.Application.Dtos;

public class SyncTriggerResponseDTO
{
    public bool WasSkipped { get; set; }
    public int LocalListsCreated { get; set; }
    public int LocalListsUpdated { get; set; }
    public int LocalListsDeleted { get; set; }
    public int ExternalListsCreated { get; set; }
    public int ExternalListsUpdated { get; set; }
    public int ExternalListsDeleted { get; set; }
    public int LocalItemsCreated { get; set; }
    public int LocalItemsUpdated { get; set; }
    public int LocalItemsDeleted { get; set; }
    public int ExternalItemsUpdated { get; set; }

    public static SyncTriggerResponseDTO From(SyncResult result) =>
        new()
        {
            WasSkipped = result.WasSkipped,
            LocalListsCreated = result.LocalListsCreated,
            LocalListsUpdated = result.LocalListsUpdated,
            LocalListsDeleted = result.LocalListsDeleted,
            ExternalListsCreated = result.ExternalListsCreated,
            ExternalListsUpdated = result.ExternalListsUpdated,
            ExternalListsDeleted = result.ExternalListsDeleted,
            LocalItemsCreated = result.LocalItemsCreated,
            LocalItemsUpdated = result.LocalItemsUpdated,
            LocalItemsDeleted = result.LocalItemsDeleted,
            ExternalItemsUpdated = result.ExternalItemsUpdated,
        };
}
