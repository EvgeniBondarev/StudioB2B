using System.Text.Json.Serialization;
using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared.DTOs;

public class OzonRatingDto
{
    [JsonPropertyName("current_value")]
    public OzonRatingValueDto? CurrentValue { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("past_value")]
    public OzonRatingValueDto? PastValue { get; set; }

    [JsonPropertyName("rating")]
    public string? Rating { get; set; }

    [JsonPropertyName("status")]
    public OzonRatingStatusEnum Status { get; set; }

    [JsonPropertyName("value_type")]
    public OzonRatingValueTypeEnum ValueType { get; set; }
}

