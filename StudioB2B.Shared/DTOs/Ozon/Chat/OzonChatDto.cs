using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonChatDto
{
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("chat_status")]
    public string ChatStatus { get; set; } = string.Empty;

    [JsonPropertyName("chat_type")]
    public string ChatType { get; set; } = string.Empty;
}
