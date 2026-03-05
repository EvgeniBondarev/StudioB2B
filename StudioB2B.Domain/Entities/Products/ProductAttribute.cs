using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Products;

/// <summary>
/// Тип атрибута товара (например «Цвет», «Размер», «Материал»).
/// </summary>
[Display(Name = "Атрибут товара")]
public class ProductAttribute : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор атрибута")]
    public Guid Id { get; set; }

    /// <summary>Код атрибута (внешний идентификатор из Ozon).</summary>
    [Display(Name = "Код атрибута")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Наименование атрибута")]
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
