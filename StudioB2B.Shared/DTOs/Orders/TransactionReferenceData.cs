using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared;

/// <summary>
/// Справочные данные для страницы проведения документа.
/// Каждый список заполняется только если соответствующий тип справочника запрошен.
/// </summary>
public class TransactionReferenceData
{
    public List<OrderStatus> OrderStatuses { get; set; } = [];

    public List<Product> Products { get; set; } = [];

    public List<Supplier> Suppliers { get; set; } = [];

    public List<Warehouse> Warehouses { get; set; } = [];

    public List<DeliveryMethod> DeliveryMethods { get; set; } = [];

    public List<Recipient> Recipients { get; set; } = [];

    public List<Address> Addresses { get; set; } = [];

    public List<WarehouseInfo> WarehouseInfos { get; set; } = [];

    public List<OrderProductInfo> OrderProductInfos { get; set; } = [];
}
