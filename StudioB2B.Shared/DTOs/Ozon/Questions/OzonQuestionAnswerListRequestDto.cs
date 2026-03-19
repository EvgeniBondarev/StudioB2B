using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonQuestionAnswerListRequestDto
{
    [JsonPropertyName("question_id")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public long Sku { get; set; }

    [JsonPropertyName("last_id")]
    public string LastId { get; set; } = string.Empty;
}
