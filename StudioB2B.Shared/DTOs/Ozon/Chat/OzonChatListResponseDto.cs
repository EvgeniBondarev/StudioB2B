using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonChatListResponseDto
{
    [JsonPropertyName("chats")]
    public List<OzonChatItemDto> Chats { get; set; } = new();

    [JsonPropertyName("total_unread_count")]
    public int TotalUnreadCount { get; set; }

    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }

    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }
}
