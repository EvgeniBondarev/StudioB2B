using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenant.Marketplace;

public class MarketplaceClientType : IHasId, IHasName, ISoftDelete
{
    [Display(Name = "Идентификатор типа клиента")]
    public Guid Id { get; set; }

    [Display(Name = "Тип клиента")]
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
