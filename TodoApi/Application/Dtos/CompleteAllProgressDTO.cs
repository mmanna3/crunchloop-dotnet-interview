namespace TodoApi.Application.Dtos;

public class CompleteAllProgressDTO
{
    public required string OperationId { get; set; }
    public long ListId { get; set; }
    public int Completed { get; set; }
    public int Total { get; set; }
    public required IReadOnlyList<long> CompletedIds { get; set; }
}
