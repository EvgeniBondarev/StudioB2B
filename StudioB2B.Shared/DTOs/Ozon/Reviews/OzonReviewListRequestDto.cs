using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewListRequestDto
{
    [JsonPropertyName("filters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OzonReviewListFiltersDto? Filters { get; set; }

    [JsonPropertyName("last_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastId { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;

    [JsonPropertyName("sort_dir")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SortDir { get; set; }
}
