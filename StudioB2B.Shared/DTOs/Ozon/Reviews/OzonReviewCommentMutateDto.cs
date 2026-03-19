using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

// ── Create ────────────────────────────────────────────────────────────────────

public class OzonReviewCommentCreateRequestDto
{
    [JsonPropertyName("mark_review_as_processed")]
    public bool MarkReviewAsProcessed { get; set; } = true;

    [JsonPropertyName("parent_comment_id")]
    public string? ParentCommentId { get; set; }

    [JsonPropertyName("review_id")]
    public string ReviewId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class OzonReviewCommentCreateResponseDto
{
    [JsonPropertyName("comment_id")]
    public string CommentId { get; set; } = string.Empty;
}

// ── Delete ────────────────────────────────────────────────────────────────────

public class OzonReviewCommentDeleteRequestDto
{
    [JsonPropertyName("comment_id")]
    public string CommentId { get; set; } = string.Empty;
}

public class OzonReviewCommentDeleteResponseDto { }

