using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFboPostingGetResponseDto
{
    [JsonPropertyName("result")]
    public OzonFboPostingDto? Result { get; set; }
}

