using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewListResponseDto
{
    [JsonPropertyName("has_next")] public bool HasNext { get; set; }
    [JsonPropertyName("last_id")] public string LastId { get; set; } = string.Empty;
    [JsonPropertyName("reviews")] public List<OzonReviewListItemDto> Reviews { get; set; } = new();
}
