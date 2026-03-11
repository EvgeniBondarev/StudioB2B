using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Связь позиции заказа с товаром и поставщиком.
/// </summary>
[Display(Name = "Товар в заказе")]
public class OrderProductInfo : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор связки заказ–товар")]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [Display(Name = "Товар")]
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    [Display(Name = "Поставщик")]
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public bool IsDeleted { get; set; }
}
