using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReadChatResponseDto
{
    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }
}
