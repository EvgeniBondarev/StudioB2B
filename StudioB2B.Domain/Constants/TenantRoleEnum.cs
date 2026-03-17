using System.ComponentModel;

namespace StudioB2B.Domain.Constants;

public enum TenantRoleEnum
{
    // Пользователи
    [Description("Пользователи: просмотр")]
    UsersView = 1,

    [Description("Пользователи: управление")]
    UsersManage = 2,

    // Заказы
    [Description("Заказы: просмотр")]
    OrdersView = 3,

    [Description("Заказы: управление")]
    OrdersManage = 4,

    // Документы заказов
    [Description("Документы заказов: просмотр")]
    TransactionsView = 5,

    [Description("Документы заказов: управление")]
    TransactionsManage = 6,

    // Статусы заказов
    [Description("Статусы заказов: просмотр")]
    OrderStatusesView = 7,

    [Description("Статусы заказов: управление")]
    OrderStatusesManage = 8,

    // Правила расчёта
    [Description("Правила расчёта: просмотр")]
    CalculationRulesView = 9,

    [Description("Правила расчёта: управление")]
    CalculationRulesManage = 10,

    // Типы цен
    [Description("Типы цен: просмотр")]
    PriceTypesView = 11,

    [Description("Типы цен: управление")]
    PriceTypesManage = 12,

    // Клиенты маркетплейсов
    [Description("Клиенты маркетплейсов: просмотр")]
    MarketplaceClientsView = 13,

    [Description("Клиенты маркетплейсов: управление")]
    MarketplaceClientsManage = 14,

    // Модули
    [Description("Модули: просмотр")]
    ModulesView = 15,

    [Description("Модули: управление")]
    ModulesManage = 16,

    // Возвраты
    [Description("Возвраты: просмотр")]
    ReturnsView = 17,

    // Чаты
    [Description("Чаты: просмотр")]
    ChatsView = 18,

    // Аудит
    [Description("Журнал аудита: просмотр")]
    AuditView = 19,
}

public static class TenantRoleHelper
{
    public static string GetDescription(TenantRoleEnum role)
    {
        var field = typeof(TenantRoleEnum).GetField(role.ToString());
        if (field is null) return role.ToString();

        var attr = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attr?.Description ?? role.ToString();
    }

    public static IEnumerable<TenantRoleEnum> AllRoles()
        => Enum.GetValues<TenantRoleEnum>();
}

