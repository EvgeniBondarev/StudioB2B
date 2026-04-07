using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonChatClosedPayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; }

    [JsonPropertyName("user")]
    public OzonPushChatUser? User { get; set; }

    [JsonPropertyName("seller_id")]
    public long SellerId { get; set; }
}

