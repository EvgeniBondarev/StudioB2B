using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

[Display(Name = "Настройки 1С клиента")]
public class MarketplaceClient1CSettings : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public Guid MarketplaceClientId { get; set; }

    public MarketplaceClient MarketplaceClient { get; set; } = null!;

    public string? INN { get; set; }

    public string? Country { get; set; }

    public string? Currency { get; set; }

    public string? LegalName { get; set; }

    public string? OzonName { get; set; }

    public string? OGRN { get; set; }

    public string? OwnershipForm { get; set; }

    public bool IsDeleted { get; set; }
}
