using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonReturnsListResponseDto
{
    [JsonPropertyName("returns")]
    public List<OzonReturnDto> Returns { get; set; } = new();

    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }
}
