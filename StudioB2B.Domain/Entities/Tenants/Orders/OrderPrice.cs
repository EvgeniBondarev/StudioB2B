using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Цена позиции заказа: тип цены (скидочная, рекомендованная и т.д.) + сумма + валюта.
/// </summary>
[Display(Name = "Цена заказа")]
public class OrderPrice : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор цены")]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public OrderEntity OrderEntity { get; set; } = null!;

    [Display(Name = "Тип цены")]
    public Guid? PriceTypeId { get; set; }
    public PriceType? PriceType { get; set; }

    [Display(Name = "Валюта")]
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    [Display(Name = "Сумма")]
    public decimal Value { get; set; }

    public bool IsDeleted { get; set; }
}
