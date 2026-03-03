using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Marketplace;

public class MarketplaceClientMode : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор режима клиента")]
    public Guid Id { get; set; }

    [Display(Name = "Режим клиента")]
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
