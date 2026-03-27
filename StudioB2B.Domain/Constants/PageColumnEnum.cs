using System.ComponentModel;

namespace StudioB2B.Domain.Constants;

/// <summary>
/// Колонки таблиц на страницах тенанта.
/// Название каждого значения используется как JWT role claim для проверки видимости колонки.
/// </summary>
public enum PageColumnEnum
{
    [Description("Заказы: колонка «Цена»")]
    OrdersViewPrice = 1,

    [Description("Заказы: видеть расчёты")]
    OrdersViewCalculations = 2,

    [Description("Заказы: колонка «№ отправления»")]
    OrdersColShipmentNumber = 3,

    [Description("Заказы: колонка «Клиент»")]
    OrdersColClient = 4,

    [Description("Заказы: колонка «Дата принятия»")]
    OrdersColAcceptedDate = 5,

    [Description("Заказы: колонка «Дата отгрузки»")]
    OrdersColShippingDate = 6,

    [Description("Заказы: колонка «Срок»")]
    OrdersColDeliveryTerm = 7,

    [Description("Заказы: колонка «Артикул»")]
    OrdersColArticle = 8,

    [Description("Заказы: колонка «Товар»")]
    OrdersColProduct = 9,

    [Description("Заказы: колонка «Производитель»")]
    OrdersColManufacturer = 10,

    [Description("Заказы: колонка «Кол-во»")]
    OrdersColQuantity = 11,

    [Description("Заказы: колонка «Статус МП»")]
    OrdersColMarketplaceStatus = 12,

    [Description("Заказы: колонка «Системный статус»")]
    OrdersColAppStatus = 13,

    [Description("Заказы: колонка «Доставка»")]
    OrdersColDeliveryMethod = 14,

    [Description("Заказы: колонка «Склад»")]
    OrdersColWarehouse = 15,

    [Description("Пользователи: колонка «Фамилия»")]
    UsersColLastName = 16,

    [Description("Пользователи: колонка «Имя»")]
    UsersColFirstName = 17,

    [Description("Пользователи: колонка «Отчество»")]
    UsersColMiddleName = 18,

    [Description("Пользователи: колонка «Email»")]
    UsersColEmail = 19,

    [Description("Пользователи: колонка «Активен»")]
    UsersColIsActive = 20,

    [Description("Пользователи: колонка «Права»")]
    UsersColPermissions = 21,

    [Description("Маркетплейс: колонка «Название»")]
    MktClientsColName = 22,

    [Description("Маркетплейс: колонка «API ID»")]
    MktClientsColApiId = 23,

    [Description("Маркетплейс: колонка «API ключ»")]
    MktClientsColApiKey = 24,

    [Description("Маркетплейс: колонка «Тип»")]
    MktClientsColType = 25,

    [Description("Маркетплейс: колонка «Режим»")]
    MktClientsColMode = 26,

    [Description("Маркетплейс: колонка «Компания»")]
    MktClientsColCompany = 27,

    [Description("Маркетплейс: колонка «ИНН»")]
    MktClientsColINN = 28,

    [Description("Статусы: колонка «Название»")]
    OStatusColName = 29,

    [Description("Статусы: колонка «Цвет»")]
    OStatusColColor = 30,

    [Description("Статусы: колонка «Конечный»")]
    OStatusColIsTerminal = 31,

    [Description("Статусы: колонка «Системный»")]
    OStatusColIsInternal = 32,

    [Description("Статусы: колонка «Синоним API»")]
    OStatusColSynonym = 33,

    [Description("Статусы: колонка «Тип клиента»")]
    OStatusColClientType = 34,

    [Description("Возвраты: колонка «ID возврата»")]
    ReturnsColReturnId = 35,

    [Description("Возвраты: колонка «№ отправления»")]
    ReturnsColPostingNumber = 36,

    [Description("Возвраты: колонка «Дата возврата»")]
    ReturnsColReturnDate = 37,

    [Description("Возвраты: колонка «Товар»")]
    ReturnsColProduct = 38,

    [Description("Возвраты: колонка «Кол-во»")]
    ReturnsColQuantity = 39,

    [Description("Возвраты: колонка «Тип»")]
    ReturnsColType = 40,

    [Description("Возвраты: колонка «Схема»")]
    ReturnsColSchema = 41,

    [Description("Возвраты: колонка «Статус возврата»")]
    ReturnsColVisualStatus = 42,

    [Description("Возвраты: колонка «Причина»")]
    ReturnsColReason = 43,

    [Description("Возвраты: колонка «Цена товара»")]
    ReturnsColPrice = 44,

    [Description("Возвраты: колонка «% комиссии»")]
    ReturnsColCommission = 45,

    [Description("Возвраты: колонка «Компенсация»")]
    ReturnsColCompensation = 46,

    [Description("Возвраты: колонка «Заказ»")]
    ReturnsColOrder = 47,

    [Description("Возвраты: колонка «Дата синхр.»")]
    ReturnsColSyncedAt = 48,

    [Description("Правила расчёта: колонка «№»")]
    CalcRulesColSortOrder = 49,

    [Description("Правила расчёта: колонка «Название»")]
    CalcRulesColName = 50,

    [Description("Правила расчёта: колонка «Ключ результата»")]
    CalcRulesColResultKey = 51,

    [Description("Правила расчёта: колонка «Формула»")]
    CalcRulesColFormula = 52,

    [Description("Правила расчёта: колонка «Активно»")]
    CalcRulesColIsActive = 53,

    [Description("Документы: колонка «Название»")]
    TransColName = 54,

    [Description("Документы: колонка «Иконка»")]
    TransColIcon = 55,

    [Description("Документы: колонка «Исходный статус»")]
    TransColFromStatus = 56,

    [Description("Документы: колонка «Целевой статус»")]
    TransColToStatus = 57,

    [Description("Документы: колонка «Правил»")]
    TransColRulesCount = 58,

    [Description("Документы: колонка «Активна»")]
    TransColIsEnabled = 59,

    [Description("История проведений: колонка «Документ»")]
    TransHistColDocument = 60,

    [Description("История проведений: колонка «Заказ»")]
    TransHistColOrder = 61,

    [Description("История проведений: колонка «Время»")]
    TransHistColTime = 62,

    [Description("История проведений: колонка «Результат»")]
    TransHistColResult = 63,

    [Description("История проведений: колонка «Пользователь»")]
    TransHistColUser = 64,

    [Description("Аудит: колонка «Дата»")]
    AuditColDate = 65,

    [Description("Аудит: колонка «Операция»")]
    AuditColChangeType = 66,

    [Description("Аудит: колонка «Сущность»")]
    AuditColEntity = 67,

    [Description("Аудит: колонка «ID записи»")]
    AuditColEntityId = 68,

    [Description("Аудит: колонка «Поле»")]
    AuditColField = 69,

    [Description("Аудит: колонка «Было»")]
    AuditColOldValue = 70,

    [Description("Аудит: колонка «Стало»")]
    AuditColNewValue = 71,

    [Description("Аудит: колонка «Кто изменил»")]
    AuditColUser = 72,

    [Description("Типы цен: колонка «Название»")]
    PriceTypesColName = 73,

    [Description("Типы цен: колонка «Схема доставки»")]
    PriceTypesColDeliveryScheme = 74,

    [Description("Типы цен: колонка «Пользовательский»")]
    PriceTypesColIsUserDefined = 75,

    [Description("Загрузка: колонка «Тип»")]
    SyncColType = 76,

    [Description("Загрузка: колонка «Статус»")]
    SyncColStatus = 77,

    [Description("Загрузка: колонка «Параметры»")]
    SyncColParams = 78,

    [Description("Загрузка: колонка «Запуск»")]
    SyncColStartedAt = 79,

    [Description("Загрузка: колонка «Завершено»")]
    SyncColFinishedAt = 80,

    [Description("Загрузка: колонка «Продолжительность»")]
    SyncColDuration = 81,

    [Description("Загрузка: колонка «Запустил»")]
    SyncColInitiatedBy = 82,
}

