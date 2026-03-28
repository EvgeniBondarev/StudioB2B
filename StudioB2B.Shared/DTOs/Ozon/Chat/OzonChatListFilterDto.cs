using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonChatListFilterDto
{
    [JsonPropertyName("chat_status")]
    public string? ChatStatus { get; set; }

    [JsonPropertyName("unread_only")]
    public bool? UnreadOnly { get; set; }
}
