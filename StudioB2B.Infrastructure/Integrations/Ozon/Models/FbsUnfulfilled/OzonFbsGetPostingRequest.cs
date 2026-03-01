using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.FbsUnfulfilled;

public class OzonFbsGetPostingRequest
{
    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("with")]
    public OzonFbsGetPostingWith With { get; set; } = new();
}

public class OzonFbsGetPostingWith
{
    [JsonPropertyName("analytics_data")]
    public bool AnalyticsData { get; set; } = false;

    [JsonPropertyName("barcodes")]
    public bool Barcodes { get; set; } = false;

    [JsonPropertyName("financial_data")]
    public bool FinancialData { get; set; } = false;

    [JsonPropertyName("translit")]
    public bool Translit { get; set; } = false;
}
