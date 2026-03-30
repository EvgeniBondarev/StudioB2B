namespace StudioB2B.Shared;

/// <summary>
/// Страница отзывов с курсором для пагинации.
/// </summary>
public class OzonReviewPageDto
{
    public List<OzonReviewViewModelDto> Reviews { get; set; } = new();

    public string? NextCursor { get; set; }

    public bool HasNext => !string.IsNullOrEmpty(NextCursor);
}

