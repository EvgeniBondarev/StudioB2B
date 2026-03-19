using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonQuestionTopSkuResponseDto
{
    [JsonPropertyName("sku")]
    public List<long> Sku { get; set; } = new();
}
