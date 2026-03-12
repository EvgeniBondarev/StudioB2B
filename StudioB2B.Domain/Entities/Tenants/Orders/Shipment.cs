using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Отправление (posting в терминах Ozon API).
/// Одно отправление содержит несколько позиций заказа (<see cref="OrderEntity"/>).
/// </summary>
[Display(Name = "Отправление")]
public class Shipment : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор отправления")]
    public Guid Id { get; set; }

    /// <summary>Номер отправления в Ozon (posting_number).</summary>
    [Display(Name = "Номер отправления (posting_number)")]
    public string PostingNumber { get; set; } = string.Empty;

    /// <summary>Номер заказа (order_number).</summary>
    [Display(Name = "Номер заказа")]
    public string? OrderNumber { get; set; }

    [Display(Name = "Клиент маркетплейса")]
    public Guid MarketplaceClientId { get; set; }
    public MarketplaceClient MarketplaceClient { get; set; } = null!;

    [Display(Name = "Статус отправления")]
    public Guid? StatusId { get; set; }
    public OrderStatus? Status { get; set; }

    [Display(Name = "Метод доставки")]
    public Guid? DeliveryMethodId { get; set; }
    public DeliveryMethod? DeliveryMethod { get; set; }

    [Display(Name = "Трек-номер")]
    public string? TrackingNumber { get; set; }

    [Display(Name = "Дата создания")]
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата сбора отправления (shipment_date).</summary>
    [Display(Name = "Дата сбора отправления")]
    public DateTime? ShipmentDate { get; set; }

    /// <summary>Дата начала обработки (in_process_at).</summary>
    [Display(Name = "Дата начала обработки")]
    public DateTime? InProcessAt { get; set; }

    public bool IsDeleted { get; set; }

    public bool HasReturn { get; set; }

    public List<OrderEntity> Orders { get; set; } = [];
    public List<ShipmentDate> Dates { get; set; } = [];
    public List<OrderReturn> Returns { get; set; } = [];
}
