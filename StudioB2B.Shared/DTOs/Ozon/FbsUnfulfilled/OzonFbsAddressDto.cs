using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFbsAddressDto
{
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("address_tail")]
    public string? AddressTail { get; set; }

    [JsonPropertyName("zip_code")]
    public string? ZipCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}
