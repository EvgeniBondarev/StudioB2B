using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonReviewCommentListResponseDto
{
    [JsonPropertyName("comments")] public List<OzonReviewCommentDto> Comments { get; set; } = new();
    [JsonPropertyName("offset")]   public int                        Offset   { get; set; }
}
