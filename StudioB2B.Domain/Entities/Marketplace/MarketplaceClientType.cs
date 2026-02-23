using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace;

public class MarketplaceClientType : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
