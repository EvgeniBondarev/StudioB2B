using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Метод доставки конкретного отправления (delivery_method из Ozon API).
/// </summary>
[Display(Name = "Способ доставки")]
public class DeliveryMethod : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор метода доставки")]
    public Guid Id { get; set; }

    /// <summary>Идентификатор метода доставки из Ozon (delivery_method.id).</summary>
    [Display(Name = "Внешний ID метода доставки")]
    public long? ExternalId { get; set; }

    [Display(Name = "Метод доставки")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Схема доставки")]
    public Guid? DeliveryTypeId { get; set; }
    public DeliveryType? DeliveryType { get; set; }

    public bool IsDeleted { get; set; }
}
