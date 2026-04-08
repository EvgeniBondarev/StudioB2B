using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonPingPayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }
}

