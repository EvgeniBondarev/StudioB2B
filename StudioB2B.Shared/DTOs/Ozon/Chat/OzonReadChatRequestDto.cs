using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReadChatRequestDto
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("from_message_id")]
    public ulong? FromMessageId { get; set; }
}
