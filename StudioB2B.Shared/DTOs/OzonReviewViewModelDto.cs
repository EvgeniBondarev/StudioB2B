namespace StudioB2B.Shared;

/// <summary>
/// Агрегированная ViewModel одного отзыва, используется в списке и в деталях.
/// </summary>
public class OzonReviewViewModelDto
{
    public Guid MarketplaceClientId { get; set; }

    public string MarketplaceClientName { get; set; } = string.Empty;

    // Credentials forwarded to service calls
    public string ApiId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public long Sku { get; set; }

    public int Rating { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string OrderStatus { get; set; } = string.Empty;

    public int CommentsAmount { get; set; }

    public int PhotosAmount { get; set; }

    public int VideosAmount { get; set; }

    public bool IsRatingParticipant { get; set; }

    public DateTime PublishedAt { get; set; }
}
