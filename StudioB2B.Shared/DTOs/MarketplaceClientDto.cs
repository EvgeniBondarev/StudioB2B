namespace StudioB2B.Shared.DTOs;

public class MarketplaceClientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public Guid? ClientTypeId { get; set; }
    public string? ClientTypeName { get; set; }
    public Guid? ModeId { get; set; }
    public string? ModeName { get; set; }
    public List<MarketplaceClientSettingDto>? Settings { get; set; }
    public MarketplaceClient1CSettingsDto? Settings1C { get; set; }
    public string? Company { get; set; }
    public string? Country { get; set; }
    public string? Currency { get; set; }
    public string? INN { get; set; }
    public string? LegalName { get; set; }
    public string? OzonName { get; set; }
    public string? OGRN { get; set; }
    public MarketplaceClientDto() { }
}
