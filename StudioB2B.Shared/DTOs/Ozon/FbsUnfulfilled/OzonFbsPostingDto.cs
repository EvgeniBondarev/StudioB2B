using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFbsPostingDto
{
    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("order_id")]
    public long OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("in_process_at")]
    public DateTime? InProcessAt { get; set; }

    [JsonPropertyName("shipment_date")]
    public DateTime? ShipmentDate { get; set; }

    [JsonPropertyName("delivery_method")]
    public OzonFbsDeliveryMethodDto? DeliveryMethod { get; set; }

    [JsonPropertyName("products")]
    public List<OzonFbsProductDto> Products { get; set; } = new();

    [JsonPropertyName("customer")]
    public OzonFbsCustomerDto? Customer { get; set; }

    [JsonPropertyName("financial_data")]
    public OzonFbsFinancialDataDto? FinancialData { get; set; }
}
