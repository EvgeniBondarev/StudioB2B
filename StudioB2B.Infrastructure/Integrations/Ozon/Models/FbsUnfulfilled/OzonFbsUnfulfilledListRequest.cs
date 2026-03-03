using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.FbsUnfulfilled;

public class OzonFbsUnfulfilledListRequest
{
    [JsonPropertyName("filter")]
    public OzonFbsUnfulfilledFilter Filter { get; set; } = new();

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;

    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;

    [JsonPropertyName("with")]
    public OzonFbsUnfulfilledWith With { get; set; } = new();

    [JsonPropertyName("dir")]
    public string Dir { get; set; } = "asc";
}

public class OzonFbsUnfulfilledFilter
{
    [JsonPropertyName("cutoff_from")]
    public DateTime CutoffFrom { get; set; }

    [JsonPropertyName("cutoff_to")]
    public DateTime CutoffTo { get; set; }
}

public class OzonFbsUnfulfilledWith
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
