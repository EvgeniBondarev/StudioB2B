using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonProductAttributesResponseDto
{
    [JsonPropertyName("result")]
    public List<OzonProductAttributeItemDto> Result { get; set; } = new();

    [JsonPropertyName("last_id")]
    public string? LastId { get; set; }

    [JsonPropertyName("total")]
    public long Total { get; set; }
}
