namespace StudioB2B.Shared;

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

