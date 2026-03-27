using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared.DTOs;

/// <summary>Начальные данные страницы/мастера: типы, режимы и счётчики клиентов.</summary>
public record MarketplaceClientInitData(
    List<MarketplaceClientType>   Types,
    List<MarketplaceClientMode>   Modes,
    Dictionary<Guid, int>         CountsByTypeId,
    Dictionary<Guid, int>         CountsByModeId);
