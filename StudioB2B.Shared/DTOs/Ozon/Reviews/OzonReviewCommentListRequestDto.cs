using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewCommentListRequestDto
{
    [JsonPropertyName("limit")] public int Limit { get; set; } = 100;
    [JsonPropertyName("offset")] public int Offset { get; set; } = 0;
    [JsonPropertyName("review_id")] public string ReviewId { get; set; } = string.Empty;
    [JsonPropertyName("sort_dir")] public string SortDir { get; set; } = "ASC";
}
