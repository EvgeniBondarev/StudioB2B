using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonChatHistoryResponseDto
{
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }

    [JsonPropertyName("messages")]
    public List<OzonChatMessageDto> Messages { get; set; } = new();
}
