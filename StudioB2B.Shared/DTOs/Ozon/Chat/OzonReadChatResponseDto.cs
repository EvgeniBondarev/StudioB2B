using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonReadChatResponseDto
{
    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }
}
