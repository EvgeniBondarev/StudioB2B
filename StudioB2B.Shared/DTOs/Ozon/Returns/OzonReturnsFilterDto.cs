using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReturnsFilterDto
{
    [JsonPropertyName("logistic_return_date")]
    public OzonReturnsDateFilterDto? LogisticReturnDate { get; set; }

    [JsonPropertyName("visual_status_change_moment")]
    public OzonReturnsDateFilterDto? VisualStatusChangeMoment { get; set; }

    [JsonPropertyName("storage_tariffication_start_date")]
    public OzonReturnsDateFilterDto? StorageTariffStartDate { get; set; }

    [JsonPropertyName("posting_numbers")]
    public List<string>? PostingNumbers { get; set; }

    [JsonPropertyName("return_schema")]
    public string? ReturnSchema { get; set; }

    [JsonPropertyName("visual_status_name")]
    public string? VisualStatusName { get; set; }

    [JsonPropertyName("offer_id")]
    public string? OfferId { get; set; }
}
