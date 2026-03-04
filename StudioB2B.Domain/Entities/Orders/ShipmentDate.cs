using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Дата, связанная с отправлением (например дата доставки, дата создания и т.п.).
/// </summary>
[Display(Name = "Дата отправления")]
public class ShipmentDate : IBaseEntity
{
    [Display(Name = "Идентификатор даты отправления")]
    public Guid Id { get; set; }

    public Guid ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;

    [Display(Name = "Тип даты")]
    public Guid DateTypeId { get; set; }
    public DateType DateType { get; set; } = null!;

    [Display(Name = "Значение даты")]
    public DateTime Value { get; set; }
}
