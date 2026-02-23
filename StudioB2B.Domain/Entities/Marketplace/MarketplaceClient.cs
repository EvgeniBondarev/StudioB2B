using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace;

public class MarketplaceClient : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ApiId { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public Guid? ClientTypeId { get; set; }

    public MarketplaceClientType? ClientType { get; set; }

    public Guid? ModeId { get; set; }

    public MarketplaceClientMode? Mode { get; set; }

    public List<MarketplaceClientSettings> Settings { get; set; } = new();

    public MarketplaceClient1CSettings? Settings1C { get; set; }

    public string? Company { get; set; }

    public string? Country { get; set; }

    public string? Currency { get; set; }

    public string? INN { get; set; }

    public string? LegalName { get; set; }

    public string? OzonName { get; set; }

    public string? OGRN { get; set; }

    public string? OwnershipForm { get; set; }

    public bool IsDeleted { get; set; }
}
