namespace StudioB2B.Shared;

/// <summary>Параметры постраничного запроса заказов.</summary>
public record OrderPageRequest(
    Guid? ClientId = null,
    Guid? StatusId = null,
    Guid? SystemStatusId = null,
    Guid? WarehouseId = null,
    bool HasReturn = false,
    string? SchemeType = null,
    string? SearchText = null,
    string? DynamicFilter = null,
    string? OrderBy = null,
    int Skip = 0,
    int Take = 15,
    bool FetchAll = false);
