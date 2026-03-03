using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductAttributes;

/// <summary>
/// Характеристика товара (attributes / complex_attributes) в ответе /v4/product/info/attributes.
/// </summary>
public class OzonAttributeDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("complex_id")]
    public long ComplexId { get; set; }

    [JsonPropertyName("values")]
    public List<OzonAttributeValueDto> Values { get; set; } = new();
}

/// <summary>
/// Одно значение характеристики.
/// </summary>
public class OzonAttributeValueDto
{
    [JsonPropertyName("dictionary_value_id")]
    public long DictionaryValueId { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
