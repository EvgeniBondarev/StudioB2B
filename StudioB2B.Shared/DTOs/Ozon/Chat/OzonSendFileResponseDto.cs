using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonSendFileResponseDto
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
}
