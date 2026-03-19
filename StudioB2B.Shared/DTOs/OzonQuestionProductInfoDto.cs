namespace StudioB2B.Shared.DTOs;

public class OzonQuestionProductInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string? PrimaryImage { get; set; }
    public List<string> Images { get; set; } = new();
    public string? Description { get; set; }
    public long Sku { get; set; }
    public string OfferId { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public long Weight { get; set; }
    public string? WeightUnit { get; set; }
    public long Height { get; set; }
    public long Width { get; set; }
    public long Depth { get; set; }
    public string? DimensionUnit { get; set; }
}
