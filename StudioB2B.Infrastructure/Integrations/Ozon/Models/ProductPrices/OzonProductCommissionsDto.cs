using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductPrices;

/// <summary>
/// Блок commissions из ответа /v5/product/info/prices.
/// Описывает тарифы и комиссии по FBO/FBS/rFBS.
/// </summary>
public class OzonProductCommissionsDto
{
    // FBO
    [JsonPropertyName("fbo_deliv_to_customer_amount")]
    public decimal? FboDelivToCustomerAmount { get; set; }

    [JsonPropertyName("fbo_direct_flow_trans_max_amount")]
    public decimal? FboDirectFlowTransMaxAmount { get; set; }

    [JsonPropertyName("fbo_direct_flow_trans_min_amount")]
    public decimal? FboDirectFlowTransMinAmount { get; set; }

    [JsonPropertyName("fbo_return_flow_amount")]
    public decimal? FboReturnFlowAmount { get; set; }

    // FBS
    [JsonPropertyName("fbs_deliv_to_customer_amount")]
    public decimal? FbsDelivToCustomerAmount { get; set; }

    [JsonPropertyName("fbs_direct_flow_trans_max_amount")]
    public decimal? FbsDirectFlowTransMaxAmount { get; set; }

    [JsonPropertyName("fbs_direct_flow_trans_min_amount")]
    public decimal? FbsDirectFlowTransMinAmount { get; set; }

    [JsonPropertyName("fbs_first_mile_max_amount")]
    public decimal? FbsFirstMileMaxAmount { get; set; }

    [JsonPropertyName("fbs_first_mile_min_amount")]
    public decimal? FbsFirstMileMinAmount { get; set; }

    [JsonPropertyName("fbs_return_flow_amount")]
    public decimal? FbsReturnFlowAmount { get; set; }

    // Продажные проценты
    [JsonPropertyName("sales_percent_fbo")]
    public double? SalesPercentFbo { get; set; }

    [JsonPropertyName("sales_percent_fbp")]
    public double? SalesPercentFbp { get; set; }

    [JsonPropertyName("sales_percent_fbs")]
    public double? SalesPercentFbs { get; set; }

    [JsonPropertyName("sales_percent_rfbs")]
    public double? SalesPercentRfbs { get; set; }
}

