namespace StudioB2B.Shared.DTOs;

public record MarketplaceClientDto(
    Guid Id,
    string Name,
    string ApiId,
    string Key,
    Guid? ClientTypeId,
    string? ClientTypeName,
    Guid? ModeId,
    string? ModeName,
    List<MarketplaceClientSettingDto>? Settings,
    MarketplaceClient1CSettingsDto? Settings1C,
    string? Company,
    string? Country,
    string? Currency,
    string? INN,
    string? LegalName,
    string? OzonName,
    string? OGRN)
{
    public MarketplaceClientDto()
        : this(default, string.Empty, string.Empty, string.Empty, null, null, null, null, null, null,
               null, null, null, null, null, null, null)
    {
    }
}
