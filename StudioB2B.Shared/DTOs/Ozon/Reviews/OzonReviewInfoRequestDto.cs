using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewInfoRequestDto
{
    [JsonPropertyName("review_id")] public string ReviewId { get; set; } = string.Empty;
}
