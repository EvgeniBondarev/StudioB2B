using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Транзакция заказа — переход из одного системного статуса в другой
/// с применением правил изменения цен и полей.
/// </summary>
[Display(Name = "Транзакция заказа")]
public class OrderTransaction : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор")]
    public Guid Id { get; set; }

    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Исходный статус")]
    public Guid FromSystemStatusId { get; set; }
    public OrderStatus? FromSystemStatus { get; set; }

    [Display(Name = "Целевой статус")]
    public Guid ToSystemStatusId { get; set; }
    public OrderStatus? ToSystemStatus { get; set; }

    [Display(Name = "Порядок")]
    public int SortOrder { get; set; }

    [Display(Name = "Активна")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>Цвет транзакции (HEX, например #FF5733). Используется для раскраски заказов.</summary>
    [Display(Name = "Цвет")]
    public string? Color { get; set; }

    public bool IsDeleted { get; set; }

    public List<OrderTransactionRule> Rules { get; set; } = [];
    public List<OrderTransactionFieldRule> FieldRules { get; set; } = [];
}
