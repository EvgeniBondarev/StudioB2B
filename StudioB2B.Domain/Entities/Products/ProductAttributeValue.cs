using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Products;

/// <summary>
/// Значение атрибута конкретного товара.
/// Составной первичный ключ: (ProductId, AttributeId).
/// </summary>
public class ProductAttributeValue : IBaseEntity
{
    /// <summary>Суррогатный PK — используется для совместимости с IBaseEntity.</summary>
    [Display(Name = "Идентификатор значения атрибута")]
    public Guid Id { get; set; }

    [Display(Name = "Товар")]
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Display(Name = "Атрибут")]
    public Guid AttributeId { get; set; }
    public ProductAttribute Attribute { get; set; } = null!;

    [Display(Name = "Значение атрибута")]
    public string Value { get; set; } = string.Empty;
}
