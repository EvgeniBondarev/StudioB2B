using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public sealed class OpenRouterChatCompletionsResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenRouterChoice>? Choices { get; set; }
}

