using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenant.Marketplace;

public class MarketplaceClient1CSettings : IHasId, ISoftDelete
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
