namespace TodoApi.Application.Services;

public class SyncResult
{
    public bool WasSkipped { get; init; }
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

    public static SyncResult Skipped() => new() { WasSkipped = true };
}
