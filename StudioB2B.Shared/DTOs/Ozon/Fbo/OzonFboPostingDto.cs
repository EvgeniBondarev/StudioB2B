using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFboPostingDto
{
    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("substatus")]
    public string? Substatus { get; set; }

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    // План: для FBO ShipmentDate берём `created_at`, а InProcessAt — `in_process_at`.
    [JsonPropertyName("created_at")]
    public DateTime? ShipmentDate { get; set; }

    [JsonPropertyName("in_process_at")]
    public DateTime? InProcessAt { get; set; }

    [JsonPropertyName("analytics_data")]
    public OzonFboDeliveryMethodDto? DeliveryMethod { get; set; }

    [JsonPropertyName("products")]
    public List<OzonFbsProductDto> Products { get; set; } = new();

    [JsonPropertyName("financial_data")]
    public OzonFbsFinancialDataDto? FinancialData { get; set; }

    [JsonPropertyName("legal_info")]
    public OzonFboLegalInfoDto? LegalInfo { get; set; }
}

