using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFbsFinancialDataDto
{
    [JsonPropertyName("products")]
    public List<OzonFbsFinancialProductDto> Products { get; set; } = new();
}
