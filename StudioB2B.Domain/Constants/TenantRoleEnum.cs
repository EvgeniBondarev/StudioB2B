using System.ComponentModel;

namespace StudioB2B.Domain.Constants;

public enum TenantRoleEnum
{
    /// <summary>Администратор — полный доступ ко всем разделам.</summary>
    [Description("Администратор")]
    Admin = 0,

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

    // Синхронизация / фоновые задачи
    [Description("Загрузка: просмотр истории задач")]
    SyncView = 20,

    [Description("Загрузка: запуск загрузки заказов")]
    SyncRunOrders = 21,

    [Description("Загрузка: запуск обновления статусов")]
    SyncRunStatusUpdate = 22,

    [Description("Загрузка: запуск загрузки возвратов")]
    SyncRunReturns = 23,

    [Description("Загрузка: управление расписаниями")]
    SyncManageSchedules = 24,

    // Расширенные колонки в таблице заказов
    [Description("Заказы: видеть колонку «Цена»")]
    OrdersViewPrice = 25,

    [Description("Заказы: видеть расчёты")]
    OrdersViewCalculations = 26,

    // ── Колонки: Заказы ──────────────────────────────────────────────────
    [Description("Заказы: колонка «№ отправления»")]
    OrdersColShipmentNumber = 27,

    [Description("Заказы: колонка «Клиент»")]
    OrdersColClient = 28,

    [Description("Заказы: колонка «Дата принятия»")]
    OrdersColAcceptedDate = 29,

    [Description("Заказы: колонка «Дата отгрузки»")]
    OrdersColShippingDate = 30,

    [Description("Заказы: колонка «Срок»")]
    OrdersColDeliveryTerm = 31,

    [Description("Заказы: колонка «Артикул»")]
    OrdersColArticle = 32,

    [Description("Заказы: колонка «Товар»")]
    OrdersColProduct = 33,

    [Description("Заказы: колонка «Производитель»")]
    OrdersColManufacturer = 34,

    [Description("Заказы: колонка «Кол-во»")]
    OrdersColQuantity = 35,

    [Description("Заказы: колонка «Статус МП»")]
    OrdersColMarketplaceStatus = 36,

    [Description("Заказы: колонка «Системный статус»")]
    OrdersColAppStatus = 37,

    [Description("Заказы: колонка «Доставка»")]
    OrdersColDeliveryMethod = 38,

    [Description("Заказы: колонка «Склад»")]
    OrdersColWarehouse = 39,

    // ── Колонки: Пользователи ────────────────────────────────────────────
    [Description("Пользователи: колонка «Фамилия»")]
    UsersColLastName = 40,

    [Description("Пользователи: колонка «Имя»")]
    UsersColFirstName = 41,

    [Description("Пользователи: колонка «Отчество»")]
    UsersColMiddleName = 42,

    [Description("Пользователи: колонка «Email»")]
    UsersColEmail = 43,

    [Description("Пользователи: колонка «Активен»")]
    UsersColIsActive = 44,

    [Description("Пользователи: колонка «Роли»")]
    UsersColRoles = 45,

    // ── Колонки: Маркетплейс ─────────────────────────────────────────────
    [Description("Маркетплейс: колонка «Название»")]
    MktClientsColName = 46,

    [Description("Маркетплейс: колонка «API ID»")]
    MktClientsColApiId = 47,

    [Description("Маркетплейс: колонка «API ключ»")]
    MktClientsColApiKey = 48,

    [Description("Маркетплейс: колонка «Тип»")]
    MktClientsColType = 49,

    [Description("Маркетплейс: колонка «Режим»")]
    MktClientsColMode = 50,

    [Description("Маркетплейс: колонка «Компания»")]
    MktClientsColCompany = 51,

    [Description("Маркетплейс: колонка «ИНН»")]
    MktClientsColINN = 52,

    // ── Колонки: Статусы заказов ─────────────────────────────────────────
    [Description("Статусы: колонка «Название»")]
    OStatusColName = 53,

    [Description("Статусы: колонка «Цвет»")]
    OStatusColColor = 54,

    [Description("Статусы: колонка «Конечный»")]
    OStatusColIsTerminal = 55,

    [Description("Статусы: колонка «Системный»")]
    OStatusColIsInternal = 56,

    [Description("Статусы: колонка «Синоним API»")]
    OStatusColSynonym = 57,

    [Description("Статусы: колонка «Тип клиента»")]
    OStatusColClientType = 58,

    // ── Колонки: Возвраты ────────────────────────────────────────────────
    [Description("Возвраты: колонка «ID возврата»")]
    ReturnsColReturnId = 59,

    [Description("Возвраты: колонка «№ отправления»")]
    ReturnsColPostingNumber = 60,

    [Description("Возвраты: колонка «Дата возврата»")]
    ReturnsColReturnDate = 61,

    [Description("Возвраты: колонка «Товар»")]
    ReturnsColProduct = 62,

    [Description("Возвраты: колонка «Кол-во»")]
    ReturnsColQuantity = 63,

    [Description("Возвраты: колонка «Тип»")]
    ReturnsColType = 64,

    [Description("Возвраты: колонка «Схема»")]
    ReturnsColSchema = 65,

    [Description("Возвраты: колонка «Статус возврата»")]
    ReturnsColVisualStatus = 66,

    [Description("Возвраты: колонка «Причина»")]
    ReturnsColReason = 67,

    [Description("Возвраты: колонка «Цена товара»")]
    ReturnsColPrice = 68,

    [Description("Возвраты: колонка «% комиссии»")]
    ReturnsColCommission = 69,

    [Description("Возвраты: колонка «Компенсация»")]
    ReturnsColCompensation = 70,

    [Description("Возвраты: колонка «Заказ»")]
    ReturnsColOrder = 71,

    [Description("Возвраты: колонка «Дата синхр.»")]
    ReturnsColSyncedAt = 72,

    // ── Колонки: Правила расчёта ─────────────────────────────────────────
    [Description("Правила расчёта: колонка «№»")]
    CalcRulesColSortOrder = 73,

    [Description("Правила расчёта: колонка «Название»")]
    CalcRulesColName = 74,

    [Description("Правила расчёта: колонка «Ключ результата»")]
    CalcRulesColResultKey = 75,

    [Description("Правила расчёта: колонка «Формула»")]
    CalcRulesColFormula = 76,

    [Description("Правила расчёта: колонка «Активно»")]
    CalcRulesColIsActive = 77,

    // ── Колонки: Документы ───────────────────────────────────────────────
    [Description("Документы: колонка «Название»")]
    TransColName = 78,

    [Description("Документы: колонка «Иконка»")]
    TransColIcon = 79,

    [Description("Документы: колонка «Исходный статус»")]
    TransColFromStatus = 80,

    [Description("Документы: колонка «Целевой статус»")]
    TransColToStatus = 81,

    [Description("Документы: колонка «Правил»")]
    TransColRulesCount = 82,

    [Description("Документы: колонка «Активна»")]
    TransColIsEnabled = 83,

    // ── Колонки: История проведений ──────────────────────────────────────
    [Description("История проведений: колонка «Документ»")]
    TransHistColDocument = 84,

    [Description("История проведений: колонка «Заказ»")]
    TransHistColOrder = 85,

    [Description("История проведений: колонка «Время»")]
    TransHistColTime = 86,

    [Description("История проведений: колонка «Результат»")]
    TransHistColResult = 87,

    [Description("История проведений: колонка «Пользователь»")]
    TransHistColUser = 88,

    // ── Колонки: Журнал аудита ───────────────────────────────────────────
    [Description("Аудит: колонка «Дата»")]
    AuditColDate = 89,

    [Description("Аудит: колонка «Операция»")]
    AuditColChangeType = 90,

    [Description("Аудит: колонка «Сущность»")]
    AuditColEntity = 91,

    [Description("Аудит: колонка «ID записи»")]
    AuditColEntityId = 92,

    [Description("Аудит: колонка «Поле»")]
    AuditColField = 93,

    [Description("Аудит: колонка «Было»")]
    AuditColOldValue = 94,

    [Description("Аудит: колонка «Стало»")]
    AuditColNewValue = 95,

    [Description("Аудит: колонка «Кто изменил»")]
    AuditColUser = 96,

    // ── Колонки: Типы цен ────────────────────────────────────────────────
    [Description("Типы цен: колонка «Название»")]
    PriceTypesColName = 97,

    [Description("Типы цен: колонка «Схема доставки»")]
    PriceTypesColDeliveryScheme = 98,

    [Description("Типы цен: колонка «Пользовательский»")]
    PriceTypesColIsUserDefined = 99,

    // ── Колонки: Загрузка (фоновые задачи) ──────────────────────────────
    [Description("Загрузка: колонка «Тип»")]
    SyncColType = 100,

    [Description("Загрузка: колонка «Статус»")]
    SyncColStatus = 101,

    [Description("Загрузка: колонка «Параметры»")]
    SyncColParams = 102,

    [Description("Загрузка: колонка «Запуск»")]
    SyncColStartedAt = 103,

    [Description("Загрузка: колонка «Завершено»")]
    SyncColFinishedAt = 104,

    [Description("Загрузка: колонка «Продолжительность»")]
    SyncColDuration = 105,

    [Description("Загрузка: колонка «Запустил»")]
    SyncColInitiatedBy = 106,
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
