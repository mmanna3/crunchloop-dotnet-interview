namespace TodoApi.Models;

public class TodoList
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public virtual ICollection<TodoItem> TodoItems { get; set; } = [];
}
