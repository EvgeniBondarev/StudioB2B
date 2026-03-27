using StudioB2B.Domain.Entities;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для работы с заказами тенанта на странице Orders.
/// Инкапсулирует все запросы к БД и фильтрацию по правам пользователя.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Загружает начальные данные страницы: клиентов, статусы, склады, правила расчётов.
    /// </summary>
    Task<OrderInitData> GetInitDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Постраничный запрос заказов со счётчиками и цветами транзакций.
    /// </summary>
    Task<OrderPageResult> GetOrderPageAsync(OrderPageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Загружает цвета последних транзакций для указанных заказов.
    /// Используется при in-memory сортировке по вычисляемым столбцам.
    /// </summary>
    Task<Dictionary<Guid, string?>> GetTransactionColorsAsync(
        IEnumerable<Guid> orderIds, CancellationToken ct = default);

    /// <summary>
    /// Данные для панели массового изменения статуса выбранных заказов.
    /// </summary>
    Task<OrderSelectionInfo> GetSelectionInfoAsync(
        IEnumerable<Guid> orderIds, CancellationToken ct = default);

    /// <summary>
    /// Ищет заказ по ShipmentId, затем по Id — для deep-link навигации.
    /// </summary>
    Task<OrderEntity?> FindOrderByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Загружает заказ по Id со всеми деталями — для диалога OrderDetailDialog.
    /// </summary>
    Task<OrderEntity?> GetOrderDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Загружает все заказы одного отправления — для вкладки «Отправление» в диалоге детализации.
    /// </summary>
    Task<List<OrderEntity>> GetShipmentOrdersAsync(Guid shipmentId, CancellationToken ct = default);
}

