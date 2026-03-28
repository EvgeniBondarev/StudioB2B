namespace StudioB2B.Shared;

public class OzonQuestionDetailDto
{
    public OzonQuestionViewModelDto Question { get; set; } = new();

    public OzonQuestionProductInfoDto? Product { get; set; }

    public bool QuestionInfoAvailable { get; set; } = true;

    public List<OzonQuestionAnswerDto> Answers { get; set; } = new();
}
