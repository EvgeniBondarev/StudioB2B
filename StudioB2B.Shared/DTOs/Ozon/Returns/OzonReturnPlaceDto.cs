using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonReturnPlaceDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}
