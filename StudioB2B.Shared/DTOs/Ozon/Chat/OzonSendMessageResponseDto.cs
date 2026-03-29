using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonSendMessageResponseDto
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
}
