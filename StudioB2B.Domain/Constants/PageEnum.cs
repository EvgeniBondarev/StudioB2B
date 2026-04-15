using System.ComponentModel;

namespace StudioB2B.Domain.Constants;

public enum PageEnum
{
    [Description("Заказы")]
    OrdersView = 1,

    [Description("Документы заказов")]
    TransactionsView = 2,

    [Description("Пользователи")]
    UsersView = 3,

    [Description("Статусы заказов")]
    OrderStatusesView = 4,

    [Description("Правила расчёта")]
    CalculationRulesView = 5,

    [Description("Типы цен")]
    PriceTypesView = 6,

    [Description("Клиенты маркетплейсов")]
    MarketplaceClientsView = 7,

    [Description("Модули")]
    ModulesView = 8,

    [Description("Возвраты")]
    ReturnsView = 9,

    [Description("Чаты")]
    ChatsView = 10,

    [Description("Журнал аудита")]
    AuditView = 11,

    [Description("Загрузка / синхронизация")]
    SyncView = 12,

    [Description("Права доступа")]
    PermissionsView = 15,

    [Description("Доска задач")]
    TaskBoardView = 16,

    [Description("Доска задач: администрирование")]
    TaskBoardAdmin = 17,

    [Description("Push-уведомления Ozon")]
    OzonPushView = 18,
}

