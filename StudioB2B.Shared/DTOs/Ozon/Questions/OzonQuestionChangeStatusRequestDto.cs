using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonQuestionChangeStatusRequestDto
{
    [JsonPropertyName("question_ids")]
    public List<string> QuestionIds { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
