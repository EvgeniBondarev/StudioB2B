using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewListRequestDto
{
    [JsonPropertyName("last_id")]
    public string LastId { get; set; } = string.Empty;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;

    [JsonPropertyName("sort_dir")]
    public string SortDir { get; set; } = "DESC";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ALL";
}
