using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReturnsListRequestDto
{
    [JsonPropertyName("filter")]
    public OzonReturnsFilterDto? Filter { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 500;

    [JsonPropertyName("last_id")]
    public long LastId { get; set; } = 0;
}
