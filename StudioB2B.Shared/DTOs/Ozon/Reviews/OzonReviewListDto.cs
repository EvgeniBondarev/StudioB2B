using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

// ── Request ──────────────────────────────────────────────────────────────────

public class OzonReviewListRequestDto
{
    /// <summary>Идентификатор последнего отзыва на предыдущей странице (пусто для первой).</summary>
    [JsonPropertyName("last_id")]
    public string LastId { get; set; } = string.Empty;

    /// <summary>Количество отзывов в ответе. Минимум 20, максимум 100.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;

    /// <summary>Направление сортировки: ASC или DESC.</summary>
    [JsonPropertyName("sort_dir")]
    public string SortDir { get; set; } = "DESC";

    /// <summary>Статус: ALL, UNPROCESSED, PROCESSED.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "ALL";
}

// ── Response ─────────────────────────────────────────────────────────────────

public class OzonReviewListItemDto
{
    [JsonPropertyName("comments_amount")]
    public int CommentsAmount { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("is_rating_participant")]
    public bool IsRatingParticipant { get; set; }

    [JsonPropertyName("order_status")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("photos_amount")]
    public int PhotosAmount { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    [JsonPropertyName("sku")]
    public long Sku { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("videos_amount")]
    public int VideosAmount { get; set; }
}

public class OzonReviewListResponseDto
{
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }

    [JsonPropertyName("last_id")]
    public string LastId { get; set; } = string.Empty;

    [JsonPropertyName("reviews")]
    public List<OzonReviewListItemDto> Reviews { get; set; } = new();
}

