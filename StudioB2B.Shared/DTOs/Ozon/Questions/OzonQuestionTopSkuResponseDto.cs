using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonQuestionTopSkuResponseDto
{
    [JsonPropertyName("sku")]
    public List<long> Sku { get; set; } = new();
}
