using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Products;

/// <summary>
/// Категория товара. Поддерживает иерархию через <see cref="ParentId"/>.
/// </summary>
public class Category : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор категории")]
    public Guid Id { get; set; }

    [Display(Name = "Категория")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Родительская категория (null — корневая).</summary>
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }

    public bool IsDeleted { get; set; }

    public List<Category> Children { get; set; } = [];
}
