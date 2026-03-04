using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Marketplace;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Статус заказа / отправления. Поддерживает цветовую маркировку, признак конечного статуса
/// и синоним для маппинга из API маркетплейса.
/// </summary>
[Display(Name = "Статус заказа")]
public class OrderStatus : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор статуса")]
    public Guid Id { get; set; }

    /// <summary>Отображаемое имя статуса.</summary>
    [Display(Name = "Статус")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Цвет статуса (HEX, например #FF5733). Используется для визуализации.</summary>
    [Display(Name = "Цвет")]
    public string? Color { get; set; }

    /// <summary>Является ли статус конечным (финальное состояние заказа).</summary>
    [Display(Name = "Конечный статус")]
    public bool IsTerminal { get; set; }

    /// <summary>Синоним — внешнее название статуса из API (например «awaiting_deliver»).</summary>
    [Display(Name = "Синоним")]
    public string? Synonym { get; set; }

    /// <summary>
    /// Признак того, что статус является чисто внутренним (бизнес-логика системы),
    /// а не напрямую отражением статуса маркетплейса.
    /// </summary>
    [Display(Name = "Внутренний статус")]
    public bool IsInternal { get; set; }

    /// <summary>
    /// Если статус связан с конкретным типом маркетплейса (например, Ozon, WB),
    /// здесь хранится ссылка на <see cref="MarketplaceClientType"/>.
    /// Для чисто внутренних статусов поле пустое.
    /// </summary>
    [Display(Name = "Тип клиента маркетплейса")]
    public Guid? MarketplaceClientTypeId { get; set; }
    public MarketplaceClientType? MarketplaceClientType { get; set; }

    public bool IsDeleted { get; set; }

    public List<StatusColor> Colors { get; set; } = [];
}
