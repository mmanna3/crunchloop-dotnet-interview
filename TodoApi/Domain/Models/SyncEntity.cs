namespace TodoApi.Domain.Models;

public abstract class SyncEntity
{
    public string? ExternalId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}
