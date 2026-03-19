using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonQuestionAnswerCreateResponseDto
{
    [JsonPropertyName("answer_id")]
    public string AnswerId { get; set; } = string.Empty;
}
