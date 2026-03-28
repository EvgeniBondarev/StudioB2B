namespace StudioB2B.Shared;

/// <summary>Параметры постраничного запроса возвратов.</summary>
public record ReturnsPageRequest(
    string? SearchText = null,
    string? DynamicFilter = null,
    string? FilterType = null,
    string? FilterSchema = null,
    bool FilterLinkedToOrder = false,
    string? OrderBy = null,
    int Skip = 0,
    int Take = 20);
