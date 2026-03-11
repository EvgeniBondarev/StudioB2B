using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Позиция заказа внутри отправления — конкретный товар с количеством и ценами.
/// </summary>
[Display(Name = "Заказ")]
public class Order : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор позиции заказа")]
    public Guid Id { get; set; }

    [Display(Name = "Отправление")]
    public Guid ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;

    /// <summary>ID заказа в Ozon (order_id).</summary>
    [Display(Name = "ID заказа в Ozon")]
    public long? OzonOrderId { get; set; }

    [Display(Name = "Количество")]
    public int Quantity { get; set; }

    /// <summary>Статус самого заказа (из API маркетплейса).</summary>
    [Display(Name = "Статус заказа (внешний)")]
    public Guid? StatusId { get; set; }
    public OrderStatus? Status { get; set; }

    /// <summary>Статус заказа во внутренней системе (системный статус).</summary>
    [Display(Name = "Статус заказа (системный)")]
    public Guid? SystemStatusId { get; set; }
    public OrderStatus? SystemStatus { get; set; }

    public Guid? ProductInfoId { get; set; }
    public OrderProductInfo? ProductInfo { get; set; }

    public Guid? RecipientId { get; set; }
    public Recipient? Recipient { get; set; }

    public Guid? WarehouseInfoId { get; set; }
    public WarehouseInfo? WarehouseInfo { get; set; }

    public bool IsDeleted { get; set; }

    public bool HasReturn { get; set; }

    public List<OrderPrice> Prices { get; set; } = [];

    public List<OrderReturn> Returns { get; set; } = [];
}
