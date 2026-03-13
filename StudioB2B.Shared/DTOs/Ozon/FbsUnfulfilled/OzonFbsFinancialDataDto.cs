using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFbsFinancialDataDto
{
    [JsonPropertyName("products")]
    public List<OzonFbsFinancialProductDto> Products { get; set; } = new();
}
