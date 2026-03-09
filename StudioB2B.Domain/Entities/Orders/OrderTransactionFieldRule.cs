using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Правило транзакции заказа: изменение поля заказа или связанной сущности при переходе статуса.
/// </summary>
[Display(Name = "Правило изменения поля")]
public class OrderTransactionFieldRule
{
    [Display(Name = "Идентификатор")]
    public Guid Id { get; set; }

    [Display(Name = "Транзакция")]
    public Guid OrderTransactionId { get; set; }
    public OrderTransaction? OrderTransaction { get; set; }

    /// <summary>Путь к полю: "Order.Quantity", "Shipment.TrackingNumber", "OrderProductInfo.SupplierId".</summary>
    [Display(Name = "Поле")]
    public string EntityPath { get; set; } = string.Empty;

    [Display(Name = "Источник значения")]
    public TransactionFieldValueSource ValueSource { get; set; }

    /// <summary>Фиксированное значение (строка, парсится по типу поля при применении).</summary>
    [Display(Name = "Фиксированное значение")]
    public string? FixedValue { get; set; }

    [Display(Name = "Порядок")]
    public int SortOrder { get; set; }

    [Display(Name = "Обязательное")]
    public bool IsRequired { get; set; }
}
