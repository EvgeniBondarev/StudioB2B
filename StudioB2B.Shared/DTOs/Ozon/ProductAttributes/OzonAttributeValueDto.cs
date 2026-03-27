using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

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
