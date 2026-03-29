using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonQuestionInfoRequestDto
{
    [JsonPropertyName("question_id")]
    public string QuestionId { get; set; } = string.Empty;
}
