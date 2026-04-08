using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonNewPostingPayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("products")]
    public List<OzonPushPostingProduct> Products { get; set; } = new();

    [JsonPropertyName("in_process_at")]
    public DateTime? InProcessAt { get; set; }

    [JsonPropertyName("warehouse_id")]
    public long WarehouseId { get; set; }

    [JsonPropertyName("shipment_date")]
    public DateTime? ShipmentDate { get; set; }

    [JsonPropertyName("tpl_integration_type")]
    public string? TplIntegrationType { get; set; }

    [JsonPropertyName("is_express")]
    public bool IsExpress { get; set; }

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("delivery_date_begin")]
    public DateTime? DeliveryDateBegin { get; set; }

    [JsonPropertyName("delivery_date_end")]
    public DateTime? DeliveryDateEnd { get; set; }

    [JsonPropertyName("seller_id")]
    public long SellerId { get; set; }
}

