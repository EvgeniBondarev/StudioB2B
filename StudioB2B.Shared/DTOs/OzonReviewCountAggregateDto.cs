namespace StudioB2B.Shared;

/// <summary>
/// Суммарное количество отзывов по статусам (агрегировано по всем клиентам).
/// </summary>
public class OzonReviewCountAggregateDto
{
    public int Processed { get; set; }

    public int Unprocessed { get; set; }

    public int Total { get; set; }
}

