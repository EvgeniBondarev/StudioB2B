namespace StudioB2B.Shared.DTOs;

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

/// <summary>
/// Детальная информация отзыва, включая фото, видео и комментарии.
/// </summary>
public class OzonReviewDetailDto
{
    public OzonReviewViewModelDto Review { get; set; } = new();
    public List<OzonReviewPhotoDto> Photos { get; set; } = new();
    public List<OzonReviewVideoDto> Videos { get; set; } = new();
    public int LikesAmount { get; set; }
    public int DislikesAmount { get; set; }
    public List<OzonReviewCommentDto> Comments { get; set; } = new();
    /// <summary>Информация о товаре, на который оставлен отзыв (из /v4/product/info/attributes).</summary>
    public OzonQuestionProductInfoDto? Product { get; set; }
}

/// <summary>
/// Страница отзывов с курсором для пагинации.
/// </summary>
public class OzonReviewPageDto
{
    public List<OzonReviewViewModelDto> Reviews { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasNext => !string.IsNullOrEmpty(NextCursor);
}

/// <summary>
/// Суммарное количество отзывов по статусам (агрегировано по всем клиентам).
/// </summary>
public class OzonReviewCountAggregateDto
{
    public int Processed { get; set; }
    public int Unprocessed { get; set; }
    public int Total { get; set; }
}

