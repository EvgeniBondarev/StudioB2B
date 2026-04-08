using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonStateChangedPayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("new_state")]
    public string? NewState { get; set; }

    [JsonPropertyName("changed_state_date")]
    public DateTime? ChangedStateDate { get; set; }

    [JsonPropertyName("warehouse_id")]
    public long WarehouseId { get; set; }

    [JsonPropertyName("seller_id")]
    public long SellerId { get; set; }
}

