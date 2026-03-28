using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonSendFileRequestDto
{
    [JsonPropertyName("base64_content")]
    public string Base64Content { get; set; } = string.Empty;

    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
