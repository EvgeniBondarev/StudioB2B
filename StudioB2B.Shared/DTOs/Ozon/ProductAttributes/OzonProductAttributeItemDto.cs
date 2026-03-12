using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

/// <summary>
/// Один товар из ответа /v4/product/info/attributes.
/// </summary>
public class OzonProductAttributeItemDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("offer_id")]
    public string OfferId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [JsonPropertyName("barcodes")]
    public List<string>? Barcodes { get; set; }

    [JsonPropertyName("primary_image")]
    public string? PrimaryImage { get; set; }

    [JsonPropertyName("sku")]
    public long Sku { get; set; }

    [JsonPropertyName("description_category_id")]
    public long DescriptionCategoryId { get; set; }

    [JsonPropertyName("type_id")]
    public long TypeId { get; set; }

    [JsonPropertyName("height")]
    public long Height { get; set; }

    [JsonPropertyName("depth")]
    public long Depth { get; set; }

    [JsonPropertyName("width")]
    public long Width { get; set; }

    [JsonPropertyName("weight")]
    public long Weight { get; set; }

    [JsonPropertyName("dimension_unit")]
    public string? DimensionUnit { get; set; }

    [JsonPropertyName("weight_unit")]
    public string? WeightUnit { get; set; }

    [JsonPropertyName("color_image")]
    public string? ColorImage { get; set; }

    [JsonPropertyName("attributes")]
    public List<OzonAttributeDto> Attributes { get; set; } = new();

    [JsonPropertyName("complex_attributes")]
    public List<OzonAttributeDto> ComplexAttributes { get; set; } = new();

    [JsonPropertyName("attributes_with_defaults")]
    public List<long>? AttributesWithDefaults { get; set; }
}
