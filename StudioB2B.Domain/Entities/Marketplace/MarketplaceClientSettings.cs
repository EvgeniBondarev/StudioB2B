using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace;

[Display(Name = "Настройки клиента")]
public class MarketplaceClientSettings : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public Guid MarketplaceClientId { get; set; }

    public MarketplaceClient MarketplaceClient { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
