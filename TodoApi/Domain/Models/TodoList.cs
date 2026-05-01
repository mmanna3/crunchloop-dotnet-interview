namespace TodoApi.Domain.Models;

public class TodoList : SyncEntity
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public virtual ICollection<TodoItem> TodoItems { get; set; } = [];
}
