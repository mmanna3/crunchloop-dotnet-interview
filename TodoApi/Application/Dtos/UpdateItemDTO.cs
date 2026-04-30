namespace TodoApi.Application.Dtos;

public class UpdateItemDTO
{
    public required string Description { get; set; }
    public bool IsCompleted { get; set; }
}
