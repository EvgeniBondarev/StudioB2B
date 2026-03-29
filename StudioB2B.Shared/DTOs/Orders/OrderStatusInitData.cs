using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared;

/// <summary>Начальные данные страницы статусов заказов: типы клиентов и счётчики.</summary>
public record OrderStatusInitData(
    List<MarketplaceClientType> ClientTypes,
    int CountInternal,
    int CountMarketplace,
    int CountTerminal,
    int CountNonTerminal,
    Dictionary<Guid, int> CountByClientTypeId);
