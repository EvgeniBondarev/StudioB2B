using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonReadChatRequestDto
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("from_message_id")]
    public ulong? FromMessageId { get; set; }
}
