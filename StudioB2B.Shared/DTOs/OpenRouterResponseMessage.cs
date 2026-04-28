using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public sealed class OpenRouterResponseMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

