using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace;

public class MarketplaceClientMode : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
