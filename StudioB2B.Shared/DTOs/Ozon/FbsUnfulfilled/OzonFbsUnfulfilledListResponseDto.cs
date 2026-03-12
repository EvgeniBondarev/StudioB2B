using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFbsUnfulfilledListResponseDto
{
    [JsonPropertyName("result")]
    public OzonFbsUnfulfilledResultDto? Result { get; set; }
}
