using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewInfoResponseDto
{
    [JsonPropertyName("comments_amount")]
    public int CommentsAmount { get; set; }

    [JsonPropertyName("dislikes_amount")]
    public int DislikesAmount { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("is_rating_participant")]
    public bool IsRatingParticipant { get; set; }

    [JsonPropertyName("likes_amount")]
    public int LikesAmount { get; set; }

    [JsonPropertyName("order_status")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("photos")]
    public List<OzonReviewPhotoDto> Photos { get; set; } = new();

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

    [JsonPropertyName("videos")]
    public List<OzonReviewVideoDto> Videos { get; set; } = new();

    [JsonPropertyName("videos_amount")]
    public int VideosAmount { get; set; }
}
