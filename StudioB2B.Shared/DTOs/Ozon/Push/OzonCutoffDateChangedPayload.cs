using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonCutoffDateChangedPayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("new_cutoff_date")]
    public DateTime? NewCutoffDate { get; set; }

    [JsonPropertyName("old_cutoff_date")]
    public DateTime? OldCutoffDate { get; set; }

    [JsonPropertyName("warehouse_id")]
    public long WarehouseId { get; set; }

    [JsonPropertyName("seller_id")]
    public long SellerId { get; set; }
}

