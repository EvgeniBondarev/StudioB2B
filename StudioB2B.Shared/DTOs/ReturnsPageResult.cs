using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared;

/// <summary>Результат постраничного запроса возвратов.</summary>
public record ReturnsPageResult(
    List<OrderReturn> Items,
    int TotalCount);
