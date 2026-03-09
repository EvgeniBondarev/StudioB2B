namespace StudioB2B.Application.Common;

/// <summary>
/// Тип значения поля для UI (выбор контрола).
/// </summary>
public enum TransactionFieldValueType
{
    String,
    Int,
    Decimal,
    DateTime,
    Guid
}

/// <summary>
/// Тип справочника для Guid-полей (выбор из списка сущностей).
/// </summary>
public enum FieldReferenceType
{
    None,
    OrderStatus,
    Product,
    Supplier,
    Warehouse,
    WarehouseInfo,
    DeliveryMethod,
    OrderProductInfo,
    Recipient,
    Address
}

/// <summary>
/// Описание поля, доступного для изменения в транзакции.
/// </summary>
public record OrderTransactionFieldDescriptor(
    string EntityPath,
    string DisplayName,
    TransactionFieldValueType ValueType,
    FieldReferenceType ReferenceType = FieldReferenceType.None);

/// <summary>
/// Реестр допустимых полей заказа и связанных сущностей для правил транзакций.
/// </summary>
public static class OrderTransactionFieldRegistry
{
    private static readonly Dictionary<string, OrderTransactionFieldDescriptor> _fields = new(StringComparer.OrdinalIgnoreCase)
    {
        // Order
        ["Order.Quantity"] = new("Order.Quantity", "Количество", TransactionFieldValueType.Int),
        ["Order.StatusId"] = new("Order.StatusId", "Статус заказа (внешний)", TransactionFieldValueType.Guid, FieldReferenceType.OrderStatus),
        ["Order.ProductInfoId"] = new("Order.ProductInfoId", "Товар в заказе", TransactionFieldValueType.Guid, FieldReferenceType.OrderProductInfo),
        ["Order.RecipientId"] = new("Order.RecipientId", "Получатель", TransactionFieldValueType.Guid, FieldReferenceType.Recipient),
        ["Order.WarehouseInfoId"] = new("Order.WarehouseInfoId", "Информация о складе", TransactionFieldValueType.Guid, FieldReferenceType.WarehouseInfo),

        // Shipment
        ["Shipment.PostingNumber"] = new("Shipment.PostingNumber", "Номер отправления", TransactionFieldValueType.String),
        ["Shipment.OrderNumber"] = new("Shipment.OrderNumber", "Номер заказа", TransactionFieldValueType.String),
        ["Shipment.StatusId"] = new("Shipment.StatusId", "Статус отправления", TransactionFieldValueType.Guid, FieldReferenceType.OrderStatus),
        ["Shipment.DeliveryMethodId"] = new("Shipment.DeliveryMethodId", "Метод доставки", TransactionFieldValueType.Guid, FieldReferenceType.DeliveryMethod),
        ["Shipment.TrackingNumber"] = new("Shipment.TrackingNumber", "Трек-номер", TransactionFieldValueType.String),
        ["Shipment.ShipmentDate"] = new("Shipment.ShipmentDate", "Дата сбора отправления", TransactionFieldValueType.DateTime),
        ["Shipment.InProcessAt"] = new("Shipment.InProcessAt", "Дата начала обработки", TransactionFieldValueType.DateTime),

        // OrderProductInfo
        ["OrderProductInfo.ProductId"] = new("OrderProductInfo.ProductId", "Товар", TransactionFieldValueType.Guid, FieldReferenceType.Product),
        ["OrderProductInfo.SupplierId"] = new("OrderProductInfo.SupplierId", "Поставщик", TransactionFieldValueType.Guid, FieldReferenceType.Supplier),

        // Recipient
        ["Recipient.Name"] = new("Recipient.Name", "Имя получателя", TransactionFieldValueType.String),
        ["Recipient.Phone"] = new("Recipient.Phone", "Телефон получателя", TransactionFieldValueType.String),
        ["Recipient.Email"] = new("Recipient.Email", "Email получателя", TransactionFieldValueType.String),
        ["Recipient.AddressId"] = new("Recipient.AddressId", "Адрес", TransactionFieldValueType.Guid, FieldReferenceType.Address),

        // WarehouseInfo
        ["WarehouseInfo.RecipientWarehouseId"] = new("WarehouseInfo.RecipientWarehouseId", "Склад-получатель", TransactionFieldValueType.Guid, FieldReferenceType.Warehouse),
        ["WarehouseInfo.SenderWarehouseId"] = new("WarehouseInfo.SenderWarehouseId", "Склад-отправитель", TransactionFieldValueType.Guid, FieldReferenceType.Warehouse)
    };

    /// <summary>Все доступные поля.</summary>
    public static IReadOnlyList<OrderTransactionFieldDescriptor> All => _fields.Values.ToList();

    /// <summary>Получить описание по пути.</summary>
    public static OrderTransactionFieldDescriptor? Get(string entityPath)
    {
        return _fields.TryGetValue(entityPath ?? string.Empty, out var d) ? d : null;
    }

    /// <summary>Проверить, что путь допустим.</summary>
    public static bool IsValid(string entityPath) => string.IsNullOrEmpty(entityPath) == false && _fields.ContainsKey(entityPath);
}
