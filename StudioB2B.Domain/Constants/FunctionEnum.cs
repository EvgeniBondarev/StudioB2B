using System.ComponentModel;

namespace StudioB2B.Domain.Constants;

/// <summary>
/// Функции (действия) на страницах тенанта.
/// Название каждого значения используется как JWT role claim.
/// </summary>
public enum FunctionEnum
{
    [Description("Пользователи: управление")]
    UsersManage = 1,

    [Description("Заказы: управление")]
    OrdersManage = 2,

    [Description("Документы заказов: управление")]
    TransactionsManage = 3,

    [Description("Статусы заказов: управление")]
    OrderStatusesManage = 4,

    [Description("Правила расчёта: управление")]
    CalculationRulesManage = 5,

    [Description("Типы цен: управление")]
    PriceTypesManage = 6,

    [Description("Клиенты маркетплейсов: управление")]
    MarketplaceClientsManage = 7,

    [Description("Модули: управление")]
    ModulesManage = 8,

    [Description("Загрузка: запуск загрузки заказов")]
    SyncRunOrders = 9,

    [Description("Загрузка: запуск обновления статусов")]
    SyncRunStatusUpdate = 10,

    [Description("Загрузка: запуск загрузки возвратов")]
    SyncRunReturns = 11,

    [Description("Загрузка: управление расписаниями")]
    SyncManageSchedules = 12,

    [Description("Вопросы: управление")]
    QuestionsManage = 13,

    [Description("Права доступа: управление")]
    PermissionsManage = 14,

    // ── Доска задач ──────────────────────────────────────────────────────
    [Description("Доска задач: управление задачами")]
    TaskBoardManage = 15,

    [Description("Доска задач: настройка тарифов и отчёты")]
    TaskBoardAdminManage = 16,
}

