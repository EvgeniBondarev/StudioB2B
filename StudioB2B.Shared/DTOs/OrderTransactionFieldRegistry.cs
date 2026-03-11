using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared.DTOs;


/// <summary>
/// Описание поля, доступного для изменения в транзакции.
/// </summary>
public record OrderTransactionFieldDescriptor(
    string EntityPath,
    string DisplayName,
    TransactionFieldValueTypeEnum ValueType,
    FieldReferenceTypeEnum ReferenceType = FieldReferenceTypeEnum.None);

/// <summary>
/// Реестр допустимых полей заказа и связанных сущностей для правил транзакций.
/// </summary>
public static class OrderTransactionFieldRegistry
{
    private static readonly Dictionary<string, OrderTransactionFieldDescriptor> _fields = new(StringComparer.OrdinalIgnoreCase)
    {
        // Order
        ["Order.Quantity"] = new("Order.Quantity", "Количество", TransactionFieldValueTypeEnum.Int),
        ["Order.StatusId"] = new("Order.StatusId", "Статус заказа (внешний)", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.OrderStatus),
        ["Order.ProductInfoId"] = new("Order.ProductInfoId", "Товар в заказе", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.OrderProductInfo),
        ["Order.RecipientId"] = new("Order.RecipientId", "Получатель", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.Recipient),
        ["Order.WarehouseInfoId"] = new("Order.WarehouseInfoId", "Информация о складе", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.WarehouseInfo),

        // Shipment
        ["Shipment.PostingNumber"] = new("Shipment.PostingNumber", "Номер отправления", TransactionFieldValueTypeEnum.String),
        ["Shipment.OrderNumber"] = new("Shipment.OrderNumber", "Номер заказа", TransactionFieldValueTypeEnum.String),
        ["Shipment.StatusId"] = new("Shipment.StatusId", "Статус отправления", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.OrderStatus),
        ["Shipment.DeliveryMethodId"] = new("Shipment.DeliveryMethodId", "Метод доставки", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.DeliveryMethod),
        ["Shipment.TrackingNumber"] = new("Shipment.TrackingNumber", "Трек-номер", TransactionFieldValueTypeEnum.String),
        ["Shipment.ShipmentDate"] = new("Shipment.ShipmentDate", "Дата сбора отправления", TransactionFieldValueTypeEnum.DateTime),
        ["Shipment.InProcessAt"] = new("Shipment.InProcessAt", "Дата начала обработки", TransactionFieldValueTypeEnum.DateTime),

        // OrderProductInfo
        ["OrderProductInfo.ProductId"] = new("OrderProductInfo.ProductId", "Товар", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.Product),
        ["OrderProductInfo.SupplierId"] = new("OrderProductInfo.SupplierId", "Поставщик", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.Supplier),

        // Recipient
        ["Recipient.Name"] = new("Recipient.Name", "Имя получателя", TransactionFieldValueTypeEnum.String),
        ["Recipient.Phone"] = new("Recipient.Phone", "Телефон получателя", TransactionFieldValueTypeEnum.String),
        ["Recipient.Email"] = new("Recipient.Email", "Email получателя", TransactionFieldValueTypeEnum.String),
        ["Recipient.AddressId"] = new("Recipient.AddressId", "Адрес", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.Address),

        // WarehouseInfo
        ["WarehouseInfo.RecipientWarehouseId"] = new("WarehouseInfo.RecipientWarehouseId", "Склад-получатель", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.Warehouse),
        ["WarehouseInfo.SenderWarehouseId"] = new("WarehouseInfo.SenderWarehouseId", "Склад-отправитель", TransactionFieldValueTypeEnum.Guid, FieldReferenceTypeEnum.Warehouse)
    };

    /// <summary>Все доступные поля.</summary>
    public static IReadOnlyList<OrderTransactionFieldDescriptor> All => _fields.Values.ToList();

    /// <summary>Получить описание по пути.</summary>
    public static OrderTransactionFieldDescriptor? Get(string? entityPath)
    {
        return _fields.TryGetValue(entityPath ?? string.Empty, out var d) ? d : null;
    }

    /// <summary>Проверить, что путь допустим.</summary>
    public static bool IsValid(string entityPath) => string.IsNullOrEmpty(entityPath) == false && _fields.ContainsKey(entityPath);
}
