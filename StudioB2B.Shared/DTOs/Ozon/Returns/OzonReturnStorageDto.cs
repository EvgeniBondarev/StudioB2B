using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

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
