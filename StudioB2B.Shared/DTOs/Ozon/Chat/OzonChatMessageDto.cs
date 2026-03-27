using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonChatMessageDto
{
    [JsonPropertyName("message_id")]
    public ulong MessageId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("data")]
    public List<string> Data { get; set; } = new();

    [JsonPropertyName("is_image")]
    public bool IsImage { get; set; }

    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }

    [JsonPropertyName("user")]
    public OzonChatMessageUserDto? User { get; set; }

    [JsonPropertyName("context")]
    public OzonChatMessageContextDto? Context { get; set; }

    [JsonPropertyName("moderate_image_status")]
    public string? ModerateImageStatus { get; set; }
}
