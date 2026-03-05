using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Products;

/// <summary>
/// Производитель товара.
/// </summary>
[Display(Name = "Производитель")]
public class Manufacturer : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор производителя")]
    public Guid Id { get; set; }

    [Display(Name = "Производитель")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Контактная информация")]
    public string? Contact { get; set; }

    [Display(Name = "Описание")]
    public string? Description { get; set; }

    public bool IsDeleted { get; set; }
}
