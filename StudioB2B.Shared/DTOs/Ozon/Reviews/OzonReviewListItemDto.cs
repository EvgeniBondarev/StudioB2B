using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewListItemDto
{
    [JsonPropertyName("comments_amount")] public int CommentsAmount { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("is_rating_participant")] public bool IsRatingParticipant { get; set; }
    [JsonPropertyName("order_status")] public string OrderStatus { get; set; } = string.Empty;
    [JsonPropertyName("photos_amount")] public int PhotosAmount { get; set; }
    [JsonPropertyName("published_at")] public DateTime PublishedAt { get; set; }
    [JsonPropertyName("rating")] public int Rating { get; set; }
    [JsonPropertyName("sku")] public long Sku { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("videos_amount")] public int VideosAmount { get; set; }
}
