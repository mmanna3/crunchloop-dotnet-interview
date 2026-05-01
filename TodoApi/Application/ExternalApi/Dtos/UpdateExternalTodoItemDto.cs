using System.Text.Json.Serialization;

namespace TodoApi.Application.ExternalApi.Dtos;

public class UpdateExternalTodoItemDto
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }
}
