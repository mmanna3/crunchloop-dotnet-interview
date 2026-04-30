namespace TodoApi.Application.Dtos;

public class TodoItemDTO
{
    public long Id { get; set; }
    public required string Description { get; set; }
    public bool IsCompleted { get; set; }
}
