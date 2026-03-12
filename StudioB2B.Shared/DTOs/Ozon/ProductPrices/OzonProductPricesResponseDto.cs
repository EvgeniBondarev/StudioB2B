using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonProductPricesResponseDto
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }

    [JsonPropertyName("items")]
    public List<OzonProductPriceItemDto> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
