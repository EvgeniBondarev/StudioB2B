using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFbsGetPostingResponseDto
{
    [JsonPropertyName("result")]
    public OzonFbsPostingDto? Result { get; set; }
}
