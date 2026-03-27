using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonChatListRequestDto
{
    [JsonPropertyName("filter")]
    public OzonChatListFilterDto? Filter { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 30;

    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}
