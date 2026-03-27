using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared.DTOs;

/// <summary>Начальные данные страницы заказов: клиенты, статусы, склады, правила.</summary>
public record OrderInitData(
    List<MarketplaceClient> Clients,
    List<OrderStatus>       MarketplaceStatuses,
    List<OrderStatus>       SystemStatuses,
    List<Warehouse>         Warehouses,
    List<CalculationRule>   CalcRules);
