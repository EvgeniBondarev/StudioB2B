using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewCommentDeleteRequestDto
{
    [JsonPropertyName("comment_id")] public string CommentId { get; set; } = string.Empty;
}
