using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFbsGetPostingResponseDto
{
    [JsonPropertyName("result")]
    public OzonFbsPostingDto? Result { get; set; }
}
