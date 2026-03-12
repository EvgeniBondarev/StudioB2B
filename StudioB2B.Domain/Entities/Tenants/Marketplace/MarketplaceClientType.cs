using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

[Display(Name = "Тип клиента")]
public class MarketplaceClientType : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор типа клиента")]
    public Guid Id { get; set; }

    [Display(Name = "Тип клиента")]
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
