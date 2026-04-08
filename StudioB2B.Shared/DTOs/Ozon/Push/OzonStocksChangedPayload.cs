using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonStocksChangedPayload
{
    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("seller_id")]
    public long SellerId { get; set; }

    [JsonPropertyName("items")]
    public List<OzonStocksChangedItem> Items { get; set; } = new();
}

public class OzonStocksChangedItem
{
    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("sku")]
    public long Sku { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("stocks")]
    public List<OzonStocksChangedStock> Stocks { get; set; } = new();
}

public class OzonStocksChangedStock
{
    [JsonPropertyName("warehouse_id")]
    public long WarehouseId { get; set; }

    [JsonPropertyName("present")]
    public long Present { get; set; }

    [JsonPropertyName("reserved")]
    public long Reserved { get; set; }
}

