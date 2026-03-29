using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewChangeStatusRequestDto
{
    [JsonPropertyName("review_ids")] public List<string> ReviewIds { get; set; } = new();

    /// <summary>PROCESSED или UNPROCESSED.</summary>
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
}
