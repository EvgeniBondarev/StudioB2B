using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonPostingCancelledPayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("products")]
    public List<OzonPushPostingProduct> Products { get; set; } = new();

    [JsonPropertyName("old_state")]
    public string? OldState { get; set; }

    [JsonPropertyName("new_state")]
    public string? NewState { get; set; }

    [JsonPropertyName("changed_state_date")]
    public DateTime? ChangedStateDate { get; set; }

    [JsonPropertyName("reason")]
    public OzonCancelReason? Reason { get; set; }

    [JsonPropertyName("warehouse_id")]
    public long WarehouseId { get; set; }

    [JsonPropertyName("seller_id")]
    public long SellerId { get; set; }
}

