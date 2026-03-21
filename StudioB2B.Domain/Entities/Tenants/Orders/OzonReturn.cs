using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Возврат/отмена заказа из Ozon API (/v1/returns/list).
/// Связывается с <see cref="Order"/> через поля <see cref="OzonOrderId"/> ↔ <see cref="OrderEntity.OzonOrderId"/>.
/// </summary>
[Display(Name = "Возврат Ozon")]
public class OrderReturn : IBaseEntity
{
    [Display(Name = "Идентификатор")]
    public Guid Id { get; set; }

    /// <summary>Ссылка на внутреннюю позицию заказа. Null, если заказ ещё не синхронизирован.</summary>
    [Display(Name = "Заказ")]
    public Guid? OrderId { get; set; }
    public OrderEntity? Order { get; set; }

    /// <summary>Ссылка на отправление. Заполняется через совпадение PostingNumber.</summary>
    [Display(Name = "Отправление")]
    public Guid? ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }

    /// <summary>Идентификатор возврата в Ozon (id из ответа API).</summary>
    [Display(Name = "ID возврата в Ozon")]
    public long OzonReturnId { get; set; }

    /// <summary>Идентификатор заказа в Ozon (order_id) — ключ для связи с Order.OzonOrderId.</summary>
    [Display(Name = "ID заказа в Ozon")]
    public long? OzonOrderId { get; set; }

    [Display(Name = "Номер заказа")]
    public string? OrderNumber { get; set; }

    [Display(Name = "Номер отправления")]
    public string? PostingNumber { get; set; }

    [Display(Name = "Источник (source_id)")]
    public long? SourceId { get; set; }

    [Display(Name = "Штрихкод отправления (clearing_id)")]
    public long? ClearingId { get; set; }

    [Display(Name = "Возвратный штрихкод (return_clearing_id)")]
    public long? ReturnClearingId { get; set; }

    /// <summary>Причина возврата.</summary>
    [Display(Name = "Причина возврата")]
    public string? ReturnReasonName { get; set; }

    /// <summary>Тип: Cancellation, FullReturn, PartialReturn, ClientReturn, Unknown.</summary>
    [Display(Name = "Тип возврата")]
    public string? Type { get; set; }

    /// <summary>Схема доставки: FBS или FBO.</summary>
    [Display(Name = "Схема")]
    public string? Schema { get; set; }

    [Display(Name = "SKU товара")]
    public long? ProductSku { get; set; }

    [Display(Name = "Артикул")]
    public string? OfferId { get; set; }

    [Display(Name = "Название товара")]
    public string? ProductName { get; set; }

    [Display(Name = "Цена товара")]
    public decimal? ProductPrice { get; set; }

    [Display(Name = "Валюта цены")]
    public string? ProductPriceCurrencyCode { get; set; }

    [Display(Name = "Цена без комиссии")]
    public decimal? ProductPriceWithoutCommission { get; set; }

    [Display(Name = "Процент комиссии")]
    public decimal? CommissionPercent { get; set; }

    [Display(Name = "Комиссия")]
    public decimal? Commission { get; set; }

    [Display(Name = "Количество")]
    public int ProductQuantity { get; set; }

    [Display(Name = "ID статуса возврата")]
    public int? VisualStatusId { get; set; }

    [Display(Name = "Статус возврата")]
    public string? VisualStatusDisplayName { get; set; }

    [Display(Name = "Системное имя статуса")]
    public string? VisualStatusSysName { get; set; }

    [Display(Name = "Дата изменения статуса")]
    public DateTime? VisualStatusChangeMoment { get; set; }

    [Display(Name = "Дата возврата покупателем")]
    public DateTime? ReturnDate { get; set; }

    [Display(Name = "Дата технического возврата")]
    public DateTime? TechnicalReturnMoment { get; set; }

    [Display(Name = "Дата прибытия на фулфилмент")]
    public DateTime? FinalMoment { get; set; }

    [Display(Name = "Дата компенсированного возврата")]
    public DateTime? CancelledWithCompensationMoment { get; set; }

    [Display(Name = "Штрихкод возвратной этикетки")]
    public string? LogisticBarcode { get; set; }

    [Display(Name = "Стоимость хранения")]
    public decimal? StorageSum { get; set; }

    [Display(Name = "Валюта хранения")]
    public string? StorageCurrencyCode { get; set; }

    [Display(Name = "Начало тарификации")]
    public DateTime? StorageTariffStartDate { get; set; }

    [Display(Name = "Дата прибытия на склад")]
    public DateTime? StorageArrivedMoment { get; set; }

    [Display(Name = "Дней ожидания")]
    public long? StorageDays { get; set; }

    [Display(Name = "Стоимость утилизации")]
    public decimal? UtilizationSum { get; set; }

    [Display(Name = "Планируемая дата утилизации")]
    public DateTime? UtilizationForecastDate { get; set; }

    [Display(Name = "Место (склад)")]
    public string? PlaceName { get; set; }

    [Display(Name = "Адрес места")]
    public string? PlaceAddress { get; set; }

    [Display(Name = "ID статуса компенсации")]
    public int? CompensationStatusId { get; set; }

    [Display(Name = "Статус компенсации")]
    public string? CompensationStatusDisplayName { get; set; }

    [Display(Name = "Дата изменения статуса компенсации")]
    public DateTime? CompensationStatusChangeMoment { get; set; }

    [Display(Name = "Вскрыт")]
    public bool IsOpened { get; set; }

    [Display(Name = "Суперэконом")]
    public bool IsSuperEconom { get; set; }

    [Display(Name = "Дата синхронизации")]
    public DateTime SyncedAt { get; set; }
}
