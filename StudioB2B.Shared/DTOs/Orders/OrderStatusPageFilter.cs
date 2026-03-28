namespace StudioB2B.Shared;

/// <summary>Параметры фильтрации статусов заказов.</summary>
public record OrderStatusPageFilter(
    string? FilterType = null,
    Guid? MarketplaceTypeId = null,
    bool? FilterTerminal = null);
