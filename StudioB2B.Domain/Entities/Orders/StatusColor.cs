using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Цветовой хэш, привязанный к статусу заказа.
/// Один статус может иметь несколько цветовых меток (например, для разных тем UI).
/// </summary>
[Display(Name = "Цвет статуса")]
public class StatusColor : IBaseEntity
{
    [Display(Name = "Идентификатор цвета статуса")]
    public Guid Id { get; set; }

    public Guid OrderStatusId { get; set; }
    public OrderStatus OrderStatus { get; set; } = null!;

    /// <summary>HEX-хэш цвета, например «#4CAF50».</summary>
    [Display(Name = "Цвет (HEX)")]
    public string Hash { get; set; } = string.Empty;
}
