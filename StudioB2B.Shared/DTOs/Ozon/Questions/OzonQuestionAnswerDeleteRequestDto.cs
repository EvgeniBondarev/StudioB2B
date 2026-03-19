using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonQuestionAnswerDeleteRequestDto
{
    [JsonPropertyName("answer_id")]
    public string AnswerId { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public long Sku { get; set; }
}
