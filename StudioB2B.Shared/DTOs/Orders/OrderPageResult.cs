using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared;

/// <summary>Результат постраничного запроса заказов.</summary>
public record OrderPageResult(
    List<OrderEntity> Items,
    int TotalCount,
    Dictionary<Guid, int> StatusCounts,
    Dictionary<Guid, int> SystemStatusCounts,
    int ReturnCount,
    Dictionary<string, int> SchemeTypeCounts,
    Dictionary<Guid, string?> TransactionColors);
