using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public sealed class OpenRouterChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

