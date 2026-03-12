using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonChatMessageUserDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
