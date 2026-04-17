# Testing Changelog

## Статус: 🟢 Фазы 1–6 завершены (код). Все unit-тесты зелёные ✅ Интеграционные тесты готовы к запуску с Docker. Бэклог завершён ✅

---

## ✅ Выполнено

### Фаза 1 — Инфраструктура
- [x] Создан `tests/StudioB2B.Tests.Unit/StudioB2B.Tests.Unit.csproj`
- [x] Создан `tests/StudioB2B.Tests.Integration/StudioB2B.Tests.Integration.csproj`
- [x] Оба проекта добавлены в `StudioB2B.sln` (папка Tests)
- [x] Создан `tests/Directory.Build.props`
- [x] Создан `docs/testing-plan.md`
- [x] Создан `docs/testing-changelog.md`

### Фаза 2 — Unit-тесты бизнес-логики (все проходят ✅)
- [x] `CalculationEngineTests` — 12 тест-кейсов: формулы, цепочки, ошибки, SanitizeKey, EvaluateFormula, ValidateFormula
- [x] `KeyEncryptionServiceTests` — 7 тест-кейсов: round-trip, разные ключи, fallback, plaintext
- [x] `CommunicationPaymentCalculatorTests` — 7 тест-кейсов: PerTask, Hourly, пол/потолок биллинга, фильтрация
- [x] `ScheduleCronBuilderTests` — 11 тест-кейсов: все типы cron-выражений, null, invalid
- [x] `DomainHelperTests` — 7 тест-кейсов: strip protocol, trailing slash, uppercase HTTPS, no protocol
- [x] `OrderSyncResultDtoTests` — 5 тест-кейсов: Add() accumulates all counter fields correctly

### Фаза 3 — Reflection-тесты системы прав
- [x] `EnumDescriptionTests` — 18+18+87 значений: все имеют `[Description]` ✅
- [x] `PageEnumCoverageTests` — 18 страниц: все защищены `[Authorize]` ✅
- [x] `FunctionEnumCoverageTests` — 14 функций: все используются в UI ✅ (4 неиспользуемых удалены)
- [x] `FunctionEnumMapTests` — 14 функций: все в `FunctionPageMap` ✅
- [x] `PageColumnEnumCoverageTests` — 87 колонок: все используются в `Col(...)` ✅
- [x] `PageColumnEnumMapTests` — 87 колонок: все в `ColumnPageMap` ✅
- [x] `NavServiceRoleTests` — все роли NavItem ссылаются на существующие PageEnum ✅

### Фаза 4 — Интеграционные тесты БД (код написан, готов к запуску с Docker)
- [x] `TenantDbContextFixture` — Testcontainers MySQL, применяет миграции
- [x] `SeedTests` — проверяет idempotent seed всех enum в БД
- [x] `PermissionCrudTests` — create/delete/duplicate permission
- [x] `AuditLogTests` — генерация FieldAuditLog, SuppressAudit
- [x] `SoftDeleteFilterTests` — фильтрация soft-delete, IgnoreQueryFilters
- [x] `UserCrudTests` — create (4 кейса): create/duplicate/update/soft-delete пользователя
- [x] `CalculationRuleCrudTests` — 5 кейсов: create/update/soft-delete + `GetActiveRulesAsync` фильтрация inactive/deleted
- [x] `OrderStatusCrudTests` — 4 кейса: create/update/soft-delete + `IgnoreQueryFilters`

### Фаза 4 — Расширение интеграционных тестов БД (новые сущности)

- [x] `TenantDbContextFixture.SeedReferenceDataAsync()` — идемпотентный метод: `MarketplaceClientType`, `MarketplaceClientMode`, `MarketplaceClient`, два `OrderStatus`. Публичные свойства `DefaultClientTypeId`, `DefaultModeId`, `DefaultClientId`, `DefaultFromStatusId`, `DefaultToStatusId`
- [x] `DatabaseSeeder` — статический класс-фабрика: `MarketplaceClient`, `Shipment`, `Order`, `OrderTransaction`, `CommunicationTask`, `TaskLog`, `Warehouse`, `WarehouseStock`, `Product`, `Manufacturer`, `PriceType`, `OzonReturn`
- [x] `MarketplaceClientCrudTests` — 5 кейсов: `GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetClientOptionsAsync` с фильтром `allowedIds`
- [x] `PriceTypeCrudTests` — 5 кейсов: create/update/soft-delete; update системного типа → `false`; пагинация с Dynamic LINQ фильтром
- [x] `OrderTransactionCrudTests` — 4 кейса: create/update/soft-delete; `GetOrderTransactionsPagedAsync` исключает удалённые
- [x] `CommunicationTaskCrudTests` — 4 кейса: create; `CommunicationTaskLog` с навигацией; soft-delete; обновление статуса
- [x] `WarehouseCrudTests` — 4 кейса: create/update/soft-delete; `WarehouseStock` с навигацией через `Include`
- [x] `ProductCrudTests` — 4 кейса: create; связь с `Manufacturer` через `Include`; soft-delete; `ProductAttributeValue` с навигацией
- [x] `OzonReturnCrudTests` — 4 кейса: create; `GetReturnsCountsAsync` группирует по `Type`; `GetReturnsPageAsync` фильтрует по тексту; счётчик отмен с `OzonOrderId`

### Фаза 4 — Дополнительные интеграционные тесты БД (расширение покрытия)

- [x] `OrderTransactionRulesTests` — 4 кейса: create с rules+fieldRules; update полностью заменяет правила; `GetOrderTransactionForEditAsync` загружает с правилами; `GetEnabledTransactionsWithStatuses` исключает отключённые; `GetCanvasData` возвращает статусы и транзакции
- [x] `OrderStatusQueryTests` — 4 кейса: пагинация с фильтром `internal`; фильтр `marketplace`; фильтр `terminal`; `GetOrderStatusInitDataAsync` возвращает корректные счётчики
- [x] `AuditLogQueryTests` — 5 кейсов: `GetAuditPagedAsync` по EntityName; по ChangeType; по диапазону дат; `GetAuditByEntityAsync` возвращает только нужные записи; `GetAuditEntityNamesAsync` содержит вставленное имя
- [x] `CommunicationPaymentRateCrudTests` — 5 кейсов: create/update/deactivate/delete; null TaskType = универсальная ставка
- [x] `ShipmentCrudTests` — 5 кейсов: create; навигация Orders через Include; soft-delete shipment; soft-delete OrderEntity; HasReturn флаг
- [x] `MarketplaceClientInitDataTests` — 4 кейса: Types содержит сеяный тип; CountsByTypeId; CountsByModeId; Modes содержит сеяный режим

### Фаза 4 — E2E и сервисные интеграционные тесты

- [x] `OrderTransactionApplyTests` — 4 кейса: успешное проведение меняет `SystemStatusId` + записывает историю; неверный статус → `Success=false` + история с ошибкой; отключённая транзакция → `Success=false`; заказ не найден → `Success=false` *(использует `OrderTransactionService` напрямую с реальным DB)*
- [x] `OrderTransactionHistoryTests` — 3 кейса: пагинация с сортировкой DESC; Dynamic LINQ фильтр по `Success==true`; пагинация (skip/take)
- [x] `OrderSelectionInfoTests` — 3 кейса: единый статус → список доступных транзакций; смешанные статусы → пустой список; `SystemStatusId=null` → `HasNullStatus=true`
- [x] `SyncJobTests` — 5 кейсов: create schedule; update cron; create history; update status `Enqueued→Succeeded`; filter by `JobType`
- [x] `BuildAuditValueResolverTests` — 3 кейса: резолвит `StatusId` → имя `OrderStatus`; резолвит `MarketplaceClientId` → имя клиента; пустые логи → пустой словарь
- [x] `PermissionQueryTests` — 4 кейса: `GetPagesWithDetailsAsync` после seed; `GetEntityOptionsForPermissionAsync` возвращает все типы сущностей; `GetAvailablePermissionsAsync` содержит созданное право; `GetPermissionByIdAsync` возвращает правильное право

### Фаза 5 — Тесты внешних сервисов
- [x] `OzonApiClientTests` — WireMock.Net: success, 401, пустые credentials

### Бэклог — Auth
- [x] `JwtTokenTests` — 6 кейсов: sub/email/roles/full_access claims, no full_access when false, issuer+audience ✅

### Бэклог — Дополнительные тесты
- [x] `OrderTransactionFieldRegistryTests` — 10 кейсов: Get/IsValid/All, case-insensitive, null, reference type, value type ✅
- [x] `OrderSyncServiceTests` — 4 кейса: single client, two clients, error swallowed, no matching clients (InMemory EF) ✅
- [x] `TenantBackupServiceTests` — 4 кейса: ConsumeDownloadToken valid/unknown/one-time-use/empty ✅
- [x] `OrderTransactionEngineTests` — 22 кейса: ApplyFieldValue (8), ValidateRequiredPriceRules (3), ValidateRequiredFieldRules (2), IsFieldValueEmpty (7) via reflection ✅
- [x] `CommunicationTaskSyncTests` — 5 кейсов: empty services, no matching tasks, Done→New reopen, exception swallowed, SyncRecentAsync ✅

### Фаза 6 — Архитектурные тесты (все проходят ✅)
- [x] `ArchitectureTests` — 4 правила NetArchTest: нет прямого ITenantDbContextFactory/IMapper в UI, нет зависимости сервисов на Web, Features — только static классы

### Исправленные баги (найдены тестами)
- [x] `FunctionEnum.UsersChangePassword` добавлен в `FunctionPageMap` в `TenantDatabaseInitializer.cs`
- [x] Удалены 4 неиспользуемых значения из `FunctionEnum`: `OrdersManage (2)`, `QuestionsManage (13)`, `TaskBoardManage (15)`, `TaskBoardAdminManage (16)`
- [x] Удалены соответствующие записи из `FunctionPageMap` в `TenantDatabaseInitializer.cs`
- [x] Удалена мёртвая seed-логика в `SeedChatManagerPermissionAsync` (назначение функций `TaskBoardManage`/`QuestionsManage`)
- [x] `NavService.cs` строка 82: `/permissions` исправлен с `ModulesView` → `PermissionsView` (тест `PermissionsPath_UsesPermissionsViewRole`)

---

## 🔄 В работе

_Фаза 4 — Интеграционные тесты БД (все написаны, запускаются с Docker)_

---

## 📋 Осталось сделать

_Бэклог полностью завершён._

---

## 📝 Известные проблемы / замечания

### Найденные тестами баги в production-коде — все исправлены ✅

| Статус | Проблема | Файл |
|---|---|---|
| ✅ Исправлено | `FunctionEnum.UsersChangePassword` отсутствовал в `FunctionPageMap` → не сидировался в БД | `TenantDatabaseInitializer.cs` |
| ✅ Удалено | `FunctionEnum.OrdersManage (2)` — определён, но нигде не использовался в UI | `FunctionEnum.cs`, `TenantDatabaseInitializer.cs` |
| ✅ Удалено | `FunctionEnum.QuestionsManage (13)` — определён, но нигде не использовался в UI | `FunctionEnum.cs`, `TenantDatabaseInitializer.cs`, seed |
| ✅ Удалено | `FunctionEnum.TaskBoardManage (15)` — определён, но нигде не использовался в UI | `FunctionEnum.cs`, `TenantDatabaseInitializer.cs`, seed |
| ✅ Удалено | `FunctionEnum.TaskBoardAdminManage (16)` — определён, но нигде не использовался в UI | `FunctionEnum.cs`, `TenantDatabaseInitializer.cs` |

### Итог запуска unit-тестов (текущий)

```
Total:   456  ✅ Passed: 456  ❌ Failed: 0
```

Прирост за сессию: +27 тестов (OrderTransactionEngineTests ×22, CommunicationTaskSyncTests ×5)

### Прочие замечания

- `ColumnPageMap` и `FunctionPageMap` — приватные поля `TenantDatabaseInitializer`.
  Тесты используют reflection для доступа к ним. Это работает.
- `JwtTokenTests` обращаются к приватному методу `AccountController.GenerateJwtToken` через reflection.

