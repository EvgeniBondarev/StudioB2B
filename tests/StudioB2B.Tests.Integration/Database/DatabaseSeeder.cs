using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Tests.Integration.Database;

/// <summary>
/// Factory helpers that build minimal valid entity instances for integration tests.
/// Returned objects are NOT yet persisted — add them to a DbContext and SaveChanges yourself.
/// Every method generates a unique name/id to avoid collisions between test runs.
/// </summary>
public static class DatabaseSeeder
{
    public static MarketplaceClient MarketplaceClient(Guid clientTypeId, string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Client_{Guid.NewGuid():N}",
            ApiId = Guid.NewGuid().ToString("N"),
            Key = Guid.NewGuid().ToString("N"),
            ClientTypeId = clientTypeId
        };

    public static Shipment Shipment(Guid marketplaceClientId) =>
        new()
        {
            Id = Guid.NewGuid(),
            PostingNumber = $"TEST-{Guid.NewGuid():N}",
            MarketplaceClientId = marketplaceClientId,
            CreatedAt = DateTime.UtcNow
        };

    public static OrderEntity Order(Guid shipmentId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Quantity = 1
        };

    public static OrderTransaction OrderTransaction(Guid fromStatusId, Guid toStatusId, string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Trans_{Guid.NewGuid():N}",
            FromSystemStatusId = fromStatusId,
            ToSystemStatusId = toStatusId,
            IsEnabled = true,
            SortOrder = 0
        };

    public static CommunicationTask CommunicationTask(Guid marketplaceClientId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExternalId = Guid.NewGuid().ToString("N"),
            MarketplaceClientId = marketplaceClientId,
            TaskType = CommunicationTaskType.Chat,
            Status = CommunicationTaskStatus.New,
            Title = $"Task_{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public static CommunicationTaskLog TaskLog(Guid taskId, string action = "Created") =>
        new()
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Action = action,
            CreatedAt = DateTime.UtcNow
        };

    public static Warehouse Warehouse(string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Warehouse_{Guid.NewGuid():N}"
        };

    public static WarehouseStock WarehouseStock(Guid warehouseId, Guid productId, int quantity = 10) =>
        new()
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            UpdatedAt = DateTime.UtcNow
        };

    public static Product Product(string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Product_{Guid.NewGuid():N}",
            Article = $"ART-{Guid.NewGuid():N}"
        };

    public static Manufacturer Manufacturer(string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Mfr_{Guid.NewGuid():N}",
            Prefix = Guid.NewGuid().ToString("N")[..4].ToUpper()
        };

    public static PriceType PriceType(bool isUserDefined = true, string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"PT_{Guid.NewGuid():N}",
            IsUserDefined = isUserDefined
        };

    public static OrderReturn OzonReturn(string type = "FullReturn", string? postingNumber = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            OzonReturnId = Random.Shared.NextInt64(1, long.MaxValue),
            Type = type,
            PostingNumber = postingNumber ?? $"RET-{Guid.NewGuid():N}",
            ProductQuantity = 1,
            SyncedAt = DateTime.UtcNow
        };
}

