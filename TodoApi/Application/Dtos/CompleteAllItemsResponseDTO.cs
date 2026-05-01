namespace TodoApi.Application.Dtos;

public class CompleteAllItemsResponseDTO
{
    public required string OperationId { get; set; }
    public int Total { get; set; }
}
