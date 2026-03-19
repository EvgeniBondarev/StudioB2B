using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

// ── Request ──────────────────────────────────────────────────────────────────

public class OzonReviewCommentListRequestDto
{
    /// <summary>Ограничение значений в ответе. Минимум 20, максимум 100.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;

    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;

    [JsonPropertyName("review_id")]
    public string ReviewId { get; set; } = string.Empty;

    /// <summary>ASC или DESC.</summary>
    [JsonPropertyName("sort_dir")]
    public string SortDir { get; set; } = "ASC";
}

// ── Response ─────────────────────────────────────────────────────────────────

public class OzonReviewCommentDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("is_official")]
    public bool IsOfficial { get; set; }

    [JsonPropertyName("is_owner")]
    public bool IsOwner { get; set; }

    [JsonPropertyName("parent_comment_id")]
    public string ParentCommentId { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class OzonReviewCommentListResponseDto
{
    [JsonPropertyName("comments")]
    public List<OzonReviewCommentDto> Comments { get; set; } = new();

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

