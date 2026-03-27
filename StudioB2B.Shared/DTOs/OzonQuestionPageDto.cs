namespace StudioB2B.Shared;

public class OzonQuestionPageDto
{
    public List<OzonQuestionViewModelDto> Questions { get; set; } = new();

    /// <summary>
    /// Курсор для следующей страницы.
    /// </summary>
    public string? NextCursor { get; set; }
}

