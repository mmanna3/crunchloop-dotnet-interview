using System.Text.Json.Serialization;

namespace TodoApi.Application.ExternalApi.Dtos;

public class UpdateExternalTodoListDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}
