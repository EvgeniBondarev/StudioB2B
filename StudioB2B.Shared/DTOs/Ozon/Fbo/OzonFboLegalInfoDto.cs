using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFboLegalInfoDto
{
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("inn")]
    public string? Inn { get; set; }

    [JsonPropertyName("kpp")]
    public string? Kpp { get; set; }
}

