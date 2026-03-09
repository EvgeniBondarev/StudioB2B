using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.References;

/// <summary>
/// Тип цены (цена продажи, скидочная цена, рекомендованная розничная и т.д.).
/// </summary>
[Display(Name = "Тип цены")]
public class PriceType : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор типа цены")]
    public Guid Id { get; set; }

    [Display(Name = "Тип цены")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Правило расчёта цены.</summary>
    [Display(Name = "Правило расчёта")]
    public string? CalculationRule { get; set; }

    /// <summary>Схема доставки, к которой применяется этот тип цен: fbs / fbo / express.</summary>
    [Display(Name = "Схема доставки")]
    public string? DeliveryScheme { get; set; }

    /// <summary>Пользовательский тип (создан вручную). Не перезаписывается при синхронизации с Ozon.</summary>
    [Display(Name = "Пользовательский")]
    public bool IsUserDefined { get; set; }

    public bool IsDeleted { get; set; }
}
