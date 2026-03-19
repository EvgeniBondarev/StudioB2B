using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonQuestionTopSkuRequestDto
{
    [JsonPropertyName("limit")]
    public long Limit { get; set; } = 100;
}
