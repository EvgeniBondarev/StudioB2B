using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFboPostingListResponseDto
{
    [JsonPropertyName("result")]
    public List<OzonFboPostingDto> Result { get; set; } = new();
}

