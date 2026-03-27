namespace StudioB2B.Shared.DTOs;

/// <summary>Счётчики типов возвратов и отмен, привязанных к заказам.</summary>
public record ReturnsCountsData(
    Dictionary<string, int> TypeCounts,
    int                     CancellationsWithOrderCount);
