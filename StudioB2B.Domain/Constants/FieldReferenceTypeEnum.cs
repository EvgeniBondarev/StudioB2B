namespace StudioB2B.Domain.Constants;

/// <summary>
/// Тип справочника для Guid-полей (выбор из списка сущностей).
/// </summary>
public enum FieldReferenceTypeEnum
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
