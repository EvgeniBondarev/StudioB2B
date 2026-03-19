using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonReturnExemplarDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
