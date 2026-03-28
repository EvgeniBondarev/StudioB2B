using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonQuestionTopSkuRequestDto
{
    [JsonPropertyName("limit")]
    public long Limit { get; set; } = 100;
}
