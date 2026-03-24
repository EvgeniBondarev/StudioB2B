using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFboPostingListResponseDto
{
    [JsonPropertyName("result")]
    public List<OzonFboPostingDto> Result { get; set; } = new();
}

