using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFbsFinancialProductDto
{
    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("commission_amount")]
    public decimal CommissionAmount { get; set; }

    [JsonPropertyName("commission_percent")]
    public decimal CommissionPercent { get; set; }

    [JsonPropertyName("old_price")]
    public decimal OldPrice { get; set; }

    [JsonPropertyName("payout")]
    public decimal Payout { get; set; }
}
