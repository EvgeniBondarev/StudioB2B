using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonDeliveryDateChangedPayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("new_delivery_date_begin")]
    public DateTime? NewDeliveryDateBegin { get; set; }

    [JsonPropertyName("new_delivery_date_end")]
    public DateTime? NewDeliveryDateEnd { get; set; }

    [JsonPropertyName("old_delivery_date_begin")]
    public DateTime? OldDeliveryDateBegin { get; set; }

    [JsonPropertyName("old_delivery_date_end")]
    public DateTime? OldDeliveryDateEnd { get; set; }

    [JsonPropertyName("warehouse_id")]
    public long WarehouseId { get; set; }

    [JsonPropertyName("seller_id")]
    public long SellerId { get; set; }
}

