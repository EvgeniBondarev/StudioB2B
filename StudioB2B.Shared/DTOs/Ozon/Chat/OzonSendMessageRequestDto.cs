using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonSendMessageRequestDto
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
