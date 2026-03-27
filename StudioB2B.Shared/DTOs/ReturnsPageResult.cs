using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared.DTOs;

/// <summary>Результат постраничного запроса возвратов.</summary>
public record ReturnsPageResult(
    List<OrderReturn> Items,
    int               TotalCount);
