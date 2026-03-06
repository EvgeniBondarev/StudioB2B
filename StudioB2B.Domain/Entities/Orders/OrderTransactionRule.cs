using System.ComponentModel.DataAnnotations;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Products;
using StudioB2B.Domain.Entities.References;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Правило транзакции заказа: установка цены при переходе статуса.
/// </summary>
[Display(Name = "Правило транзакции")]
public class OrderTransactionRule : IBaseEntity
{
    [Display(Name = "Идентификатор")]
    public Guid Id { get; set; }

    [Display(Name = "Транзакция")]
    public Guid OrderTransactionId { get; set; }
    public OrderTransaction? OrderTransaction { get; set; }

    [Display(Name = "Тип цены")]
    public Guid PriceTypeId { get; set; }
    public PriceType? PriceType { get; set; }

    [Display(Name = "Источник значения")]
    public TransactionValueSource ValueSource { get; set; }

    [Display(Name = "Фиксированное значение")]
    public decimal? FixedValue { get; set; }

    [Display(Name = "Формула")]
    public string? Formula { get; set; }

    /// <summary>Опционально: правило применяется только для указанного товара.</summary>
    [Display(Name = "Товар")]
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    [Display(Name = "Валюта")]
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    [Display(Name = "Порядок")]
    public int SortOrder { get; set; }
}
