using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public sealed class OpenRouterChoice
{
    [JsonPropertyName("message")]
    public OpenRouterResponseMessage? Message { get; set; }
}

