using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

/// <summary>Payload для TYPE_CREATE_ITEM, TYPE_UPDATE_ITEM, TYPE_CREATE_OR_UPDATE_ITEM.</summary>
public class OzonItemPayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("seller_id")]
    public long SellerId { get; set; }

    [JsonPropertyName("offer_id")]
    public string? OfferId { get; set; }

    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("is_error")]
    public bool IsError { get; set; }

    [JsonPropertyName("changed_at")]
    public DateTime? ChangedAt { get; set; }
}

