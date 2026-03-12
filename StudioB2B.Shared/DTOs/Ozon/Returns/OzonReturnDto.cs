using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

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
