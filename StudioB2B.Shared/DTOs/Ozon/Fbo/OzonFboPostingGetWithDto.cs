using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFboPostingGetWithDto
{
    [JsonPropertyName("analytics_data")]
    public bool AnalyticsData { get; set; } = true;

    [JsonPropertyName("financial_data")]
    public bool FinancialData { get; set; } = true;

    [JsonPropertyName("legal_info")]
    public bool LegalInfo { get; set; } = false;
}

