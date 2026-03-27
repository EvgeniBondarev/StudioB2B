using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFbsUnfulfilledListResponseDto
{
    [JsonPropertyName("result")]
    public OzonFbsUnfulfilledResultDto? Result { get; set; }
}
