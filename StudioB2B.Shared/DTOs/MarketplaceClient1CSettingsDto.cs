namespace StudioB2B.Shared;

public record MarketplaceClient1CSettingsDto(
    Guid Id,
    Guid MarketplaceClientId,
    string? INN,
    string? Country,
    string? Currency,
    string? LegalName,
    string? OzonName,
    string? OGRN,
    string? OwnershipForm);
