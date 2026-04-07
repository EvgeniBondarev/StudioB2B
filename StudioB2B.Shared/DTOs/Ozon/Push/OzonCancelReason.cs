using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonCancelReason
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

