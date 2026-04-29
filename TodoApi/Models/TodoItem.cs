namespace TodoApi.Models;

public class TodoItem
{
    public long Id { get; set; }
    public required string Description { get; set; }
    public bool IsCompleted { get; set; } = false;
    public long TodoListId { get; set; }
    public virtual TodoList TodoList { get; set; } = default!;
}
