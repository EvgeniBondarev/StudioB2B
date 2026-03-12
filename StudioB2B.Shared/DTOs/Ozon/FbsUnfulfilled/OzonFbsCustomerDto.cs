using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFbsCustomerDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("address")]
    public OzonFbsAddressDto? Address { get; set; }
}
