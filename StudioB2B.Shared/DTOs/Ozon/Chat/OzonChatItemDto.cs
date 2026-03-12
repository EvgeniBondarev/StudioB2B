using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonChatItemDto
{
    [JsonPropertyName("chat")]
    public OzonChatDto? Chat { get; set; }

    [JsonPropertyName("first_unread_message_id")]
    public ulong? FirstUnreadMessageId { get; set; }

    [JsonPropertyName("last_message_id")]
    public ulong? LastMessageId { get; set; }

    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }
}
