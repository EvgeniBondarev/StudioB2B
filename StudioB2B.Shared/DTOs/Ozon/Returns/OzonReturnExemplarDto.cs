using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReturnExemplarDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
