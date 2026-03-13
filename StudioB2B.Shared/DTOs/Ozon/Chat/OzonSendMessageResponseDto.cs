using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonSendMessageResponseDto
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
}
