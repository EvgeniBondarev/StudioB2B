using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

/// <summary>Payload для TYPE_NEW_MESSAGE, TYPE_UPDATE_MESSAGE, TYPE_MESSAGE_READ.</summary>
public class OzonChatMessagePayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; }

    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("user")]
    public OzonPushChatUser? User { get; set; }

    [JsonPropertyName("data")]
    public List<string> Data { get; set; } = new();

    [JsonPropertyName("last_read_message_id")]
    public string? LastReadMessageId { get; set; }

    [JsonPropertyName("seller_id")]
    public long SellerId { get; set; }
}

public class OzonPushChatUser
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

