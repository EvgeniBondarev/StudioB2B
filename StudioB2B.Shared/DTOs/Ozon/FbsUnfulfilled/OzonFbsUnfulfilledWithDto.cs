using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFbsUnfulfilledWithDto
{
    [JsonPropertyName("analytics_data")]
    public bool AnalyticsData { get; set; } = false;

    [JsonPropertyName("barcodes")]
    public bool Barcodes { get; set; } = false;

    [JsonPropertyName("financial_data")]
    public bool FinancialData { get; set; } = true;

    [JsonPropertyName("translit")]
    public bool Translit { get; set; } = false;
}
