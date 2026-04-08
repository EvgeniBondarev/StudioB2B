using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

/// <summary>Товар в уведомлениях об отправлении.</summary>
public class OzonPushPostingProduct
{
    [JsonPropertyName("sku")]
    public long Sku { get; set; }

    [JsonPropertyName("offer_id")]
    public string? OfferId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

