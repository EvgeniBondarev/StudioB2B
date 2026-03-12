using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonProductPricesRequestDto
{
    [JsonPropertyName("cursor")]
    public string Cursor { get; set; } = string.Empty;

    [JsonPropertyName("filter")]
    public OzonProductPricesFilterDto Filter { get; set; } = new();

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;
}
