namespace StudioB2B.Shared.DTOs;

/// <summary>Параметры постраничной фильтрации грида клиентов.</summary>
public record MarketplaceClientPageFilter(
    Guid?   TypeId  = null,
    Guid?   ModeId  = null,
    string? Filter  = null,
    string? OrderBy = null);
