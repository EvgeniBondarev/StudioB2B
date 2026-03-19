using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonQuestionAnswerListResponseDto
{
    [JsonPropertyName("answers")]
    public List<OzonQuestionAnswerDto> Answers { get; set; } = new();

    [JsonPropertyName("last_id")]
    public string? LastId { get; set; }
}
