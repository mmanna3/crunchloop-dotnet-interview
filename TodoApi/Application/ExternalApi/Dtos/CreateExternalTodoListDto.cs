using System.Text.Json.Serialization;

namespace TodoApi.Application.ExternalApi.Dtos;

public class CreateExternalTodoListDto
{
    [JsonPropertyName("source_id")]
    public string SourceId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("items")]
    public List<CreateExternalTodoItemDto> Items { get; set; } = [];
}
