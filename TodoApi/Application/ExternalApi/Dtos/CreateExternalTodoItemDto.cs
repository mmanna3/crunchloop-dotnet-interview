using System.Text.Json.Serialization;

namespace TodoApi.Application.ExternalApi.Dtos;

public class CreateExternalTodoItemDto
{
    [JsonPropertyName("source_id")]
    public string SourceId { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }
}
