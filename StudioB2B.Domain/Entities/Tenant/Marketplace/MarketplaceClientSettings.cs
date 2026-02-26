using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenant.Marketplace;

public class MarketplaceClientSettings : IHasId, ISoftDelete
{
    public Guid Id { get; set; }

    public Guid MarketplaceClientId { get; set; }

    public MarketplaceClient MarketplaceClient { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
