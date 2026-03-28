using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonChatHistoryRequestDto
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "Backward";

    [JsonPropertyName("from_message_id")]
    public ulong? FromMessageId { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 50;
}
