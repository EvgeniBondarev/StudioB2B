using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Правило вычисления поля «на лету» по формуле (DynamicExpresso).
/// Переменные берутся из цен заказа (PriceType.Name → SanitizeKey).
/// </summary>
[Display(Name = "Правило расчёта")]
public class CalculationRule : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор")]
    public Guid Id { get; set; }

    /// <summary>Отображаемое название правила, например «Скидка».</summary>
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ключ результата — имя переменной, под которым результат добавляется в контекст
    /// и отображается пользователю, например «Скидка».
    /// </summary>
    [Display(Name = "Ключ результата")]
    public string ResultKey { get; set; } = string.Empty;

    /// <summary>
    /// Выражение на DynamicExpresso, например «ЦенаДоСкидки - Цена» или «Цена * Quantity».
    /// </summary>
    [Display(Name = "Формула")]
    public string Formula { get; set; } = string.Empty;

    /// <summary>Порядок вычисления — меньший номер вычисляется раньше.</summary>
    [Display(Name = "Порядок")]
    public int SortOrder { get; set; }

    [Display(Name = "Активно")]
    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
}
