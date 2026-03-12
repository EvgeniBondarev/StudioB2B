using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.Returns;

// ── Request ───────────────────────────────────────────────────────────────────

public class OzonReturnsListRequest
{
    [JsonPropertyName("filter")]
    public OzonReturnsFilter? Filter { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 500;

    [JsonPropertyName("last_id")]
    public long LastId { get; set; } = 0;
}

public class OzonReturnsFilter
{
    [JsonPropertyName("logistic_return_date")]
    public OzonReturnsDateFilter? LogisticReturnDate { get; set; }

    [JsonPropertyName("visual_status_change_moment")]
    public OzonReturnsDateFilter? VisualStatusChangeMoment { get; set; }

    [JsonPropertyName("storage_tariffication_start_date")]
    public OzonReturnsDateFilter? StorageTariffStartDate { get; set; }

    [JsonPropertyName("posting_numbers")]
    public List<string>? PostingNumbers { get; set; }

    [JsonPropertyName("return_schema")]
    public string? ReturnSchema { get; set; }

    [JsonPropertyName("visual_status_name")]
    public string? VisualStatusName { get; set; }

    [JsonPropertyName("offer_id")]
    public string? OfferId { get; set; }
}

public class OzonReturnsDateFilter
{
    [JsonPropertyName("time_from")]
    public DateTime? TimeFrom { get; set; }

    [JsonPropertyName("time_to")]
    public DateTime? TimeTo { get; set; }
}

// ── Response ──────────────────────────────────────────────────────────────────

public class OzonReturnsListResponse
{
    [JsonPropertyName("returns")]
    public List<OzonReturnDto> Returns { get; set; } = new();

    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }
}

public class OzonReturnDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("company_id")]
    public long? CompanyId { get; set; }

    [JsonPropertyName("return_reason_name")]
    public string? ReturnReasonName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("order_id")]
    public long? OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("posting_number")]
    public string? PostingNumber { get; set; }

    [JsonPropertyName("source_id")]
    public long? SourceId { get; set; }

    [JsonPropertyName("clearing_id")]
    public long? ClearingId { get; set; }

    [JsonPropertyName("return_clearing_id")]
    public long? ReturnClearingId { get; set; }

    [JsonPropertyName("place")]
    public OzonReturnPlaceDto? Place { get; set; }

    [JsonPropertyName("target_place")]
    public OzonReturnPlaceDto? TargetPlace { get; set; }

    [JsonPropertyName("storage")]
    public OzonReturnStorageDto? Storage { get; set; }

    [JsonPropertyName("product")]
    public OzonReturnProductDto? Product { get; set; }

    [JsonPropertyName("logistic")]
    public OzonReturnLogisticDto? Logistic { get; set; }

    [JsonPropertyName("visual")]
    public OzonReturnVisualDto? Visual { get; set; }

    [JsonPropertyName("additional_info")]
    public OzonReturnAdditionalInfoDto? AdditionalInfo { get; set; }

    [JsonPropertyName("compensation_status")]
    public OzonReturnCompensationStatusDto? CompensationStatus { get; set; }

    [JsonPropertyName("exemplars")]
    public List<OzonReturnExemplarDto>? Exemplars { get; set; }
}

public class OzonReturnPlaceDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public class OzonReturnStorageDto
{
    [JsonPropertyName("sum")]
    public OzonReturnMoneyDto? Sum { get; set; }

    [JsonPropertyName("tariffication_first_date")]
    public DateTime? TariffFirstDate { get; set; }

    [JsonPropertyName("tariffication_start_date")]
    public DateTime? TariffStartDate { get; set; }

    [JsonPropertyName("arrived_moment")]
    public DateTime? ArrivedMoment { get; set; }

    [JsonPropertyName("days")]
    public long? Days { get; set; }

    [JsonPropertyName("utilization_sum")]
    public OzonReturnMoneyDto? UtilizationSum { get; set; }

    [JsonPropertyName("utilization_forecast_date")]
    public DateTime? UtilizationForecastDate { get; set; }
}

public class OzonReturnMoneyDto
{
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }
}

public class OzonReturnProductDto
{
    [JsonPropertyName("sku")]
    public long? Sku { get; set; }

    [JsonPropertyName("offer_id")]
    public string? OfferId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("price")]
    public OzonReturnMoneyDto? Price { get; set; }

    [JsonPropertyName("price_without_commission")]
    public OzonReturnMoneyDto? PriceWithoutCommission { get; set; }

    [JsonPropertyName("commission_percent")]
    public decimal? CommissionPercent { get; set; }

    [JsonPropertyName("commission")]
    public OzonReturnMoneyDto? Commission { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

public class OzonReturnLogisticDto
{
    [JsonPropertyName("technical_return_moment")]
    public DateTime? TechnicalReturnMoment { get; set; }

    [JsonPropertyName("final_moment")]
    public DateTime? FinalMoment { get; set; }

    [JsonPropertyName("cancelled_with_compensation_moment")]
    public DateTime? CancelledWithCompensationMoment { get; set; }

    [JsonPropertyName("return_date")]
    public DateTime? ReturnDate { get; set; }

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }
}

public class OzonReturnVisualDto
{
    [JsonPropertyName("status")]
    public OzonReturnStatusRefDto? Status { get; set; }

    [JsonPropertyName("change_moment")]
    public DateTime? ChangeMoment { get; set; }
}

public class OzonReturnStatusRefDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("sys_name")]
    public string? SysName { get; set; }
}

public class OzonReturnAdditionalInfoDto
{
    [JsonPropertyName("is_opened")]
    public bool IsOpened { get; set; }

    [JsonPropertyName("is_super_econom")]
    public bool IsSuperEconom { get; set; }
}

public class OzonReturnCompensationStatusDto
{
    [JsonPropertyName("status")]
    public OzonReturnStatusRefDto? Status { get; set; }

    [JsonPropertyName("change_moment")]
    public DateTime? ChangeMoment { get; set; }
}

public class OzonReturnExemplarDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
