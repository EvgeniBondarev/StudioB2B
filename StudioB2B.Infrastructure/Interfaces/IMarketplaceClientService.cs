using StudioB2B.Domain.Entities;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для работы с клиентами маркетплейсов тенанта.
/// </summary>
public interface IMarketplaceClientService
{
    /// <summary>
    /// Начальные данные страницы и мастера: типы, режимы, счётчики.
    /// </summary>
    Task<MarketplaceClientInitData> GetInitDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Постраничная выборка с фильтрацией и сортировкой.
    /// </summary>
    Task<(List<MarketplaceClient> Items, int Total)> GetPagedAsync(
        MarketplaceClientPageFilter filter,
        int skip,
        int take,
        CancellationToken ct = default);

    /// <summary>
    /// Обновить скалярные поля клиента.
    /// </summary>
    Task UpdateAsync(MarketplaceClient client, CancellationToken ct = default);

    /// <summary>
    /// Удалить клиента по идентификатору.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Проверить: существует ли клиент с данным ApiId.
    /// </summary>
    Task<bool> ExistsByApiIdAsync(string apiId, CancellationToken ct = default);

    /// <summary>
    /// Создать нового клиента из DTO.
    /// </summary>
    Task<MarketplaceClientDto> CreateAsync(CreateMarketplaceClientDto dto, CancellationToken ct = default);

    /// <summary>
    /// Возвращает true, если в тенанте есть хотя бы один клиент маркетплейса.
    /// </summary>
    Task<bool> HasAnyAsync(CancellationToken ct = default);

    /// <summary>
    /// Список клиентов для фильтра (Id + Name) с учётом ограничений пользователя.
    /// </summary>
    Task<List<ClientOptionDto>> GetClientOptionsAsync(CancellationToken ct = default);
}

