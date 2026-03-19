using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonQuestionAnswerCreateRequestDto
{
    [JsonPropertyName("question_id")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public long Sku { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
