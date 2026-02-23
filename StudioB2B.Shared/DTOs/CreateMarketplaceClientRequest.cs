namespace StudioB2B.Shared.DTOs;

public record CreateMarketplaceClientRequest(
    string Name,
    string ApiId,
    string Key,
    Guid? ClientTypeId,
    Guid? ModeId,
    List<MarketplaceClientSettingDto>? Settings = null,
    MarketplaceClient1CSettingsDto? Settings1C = null,
    string? Company = null,
    string? Country = null,
    string? Currency = null,
    string? INN = null,
    string? LegalName = null,
    string? OzonName = null,
    string? OGRN = null,
    string? OwnershipForm = null);
