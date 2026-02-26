using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.SellerInfo;

public class OzonCompanyDto
{
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("inn")]
    public string? Inn { get; set; }

    [JsonPropertyName("legal_name")]
    public string? LegalName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("ogrn")]
    public string? Ogrn { get; set; }

    [JsonPropertyName("ownership_form")]
    public string? OwnershipForm { get; set; }

    [JsonPropertyName("tax_system")]
    public OzonTaxSystem TaxSystem { get; set; }
}

