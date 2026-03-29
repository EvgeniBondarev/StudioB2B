namespace StudioB2B.Shared;

/// <summary>Счётчики типов возвратов и отмен, привязанных к заказам.</summary>
public record ReturnsCountsData(
    Dictionary<string, int> TypeCounts,
    int CancellationsWithOrderCount);
