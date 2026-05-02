namespace StudioB2B.Shared;

/// <summary>
/// Суммарное количество отзывов по статусам (агрегировано по всем клиентам).
/// </summary>
public class OzonReviewCountAggregateDto
{
    public int New { get; set; }

    public int Viewed { get; set; }

    public int Processed { get; set; }

    public int Total { get; set; }
}

