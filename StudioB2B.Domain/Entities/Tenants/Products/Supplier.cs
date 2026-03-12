using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Поставщик товара.
/// </summary>
[Display(Name = "Поставщик")]
public class Supplier : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор поставщика")]
    public Guid Id { get; set; }

    [Display(Name = "Поставщик")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Внутренний код поставщика.</summary>
    [Display(Name = "Код поставщика")]
    public string? Code { get; set; }

    [Display(Name = "Описание")]
    public string? Description { get; set; }

    public bool IsDeleted { get; set; }
}
