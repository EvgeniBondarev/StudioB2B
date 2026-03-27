using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonChatMessageContextDto
{
    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }
}
