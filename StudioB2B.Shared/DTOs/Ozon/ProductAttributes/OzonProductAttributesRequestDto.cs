using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonProductAttributesRequestDto
{
    [JsonPropertyName("filter")]
    public OzonProductAttributesFilterDto Filter { get; set; } = new();

    [JsonPropertyName("last_id")]
    public string LastId { get; set; } = string.Empty;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 1000;

    [JsonPropertyName("sort_dir")]
    public string SortDir { get; set; } = "ASC";
}
