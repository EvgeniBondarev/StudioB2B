using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonAttributeDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("complex_id")]
    public long ComplexId { get; set; }

    [JsonPropertyName("values")]
    public List<OzonAttributeValueDto> Values { get; set; } = new();
}
