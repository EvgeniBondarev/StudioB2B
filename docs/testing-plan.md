# Testing Plan — StudioB2B

## Цель

Добавить тестовое покрытие по шести направлениям:
бизнес-логика, система прав, БД, внешние сервисы, архитектура.

## Структура проектов

```
tests/
  StudioB2B.Tests.Unit/          ← xUnit, чистые unit-тесты без IO
  StudioB2B.Tests.Integration/   ← xUnit + Testcontainers (MySQL), WireMock.Net
```

## Фаза 1 — Инфраструктура тестовых проектов

**Цель:** создать скелет, настроить решение, CI-ready конфиг.

### Задачи

- [ ] Создать `StudioB2B.Tests.Unit` (SDK `Microsoft.NET.Sdk`, net10.0)
  - Пакеты: `xunit`, `xunit.runner.visualstudio`, `Moq`, `FluentAssertions`, `coverlet.collector`, `Microsoft.NET.Test.Sdk`
  - Ссылки на проекты: `StudioB2B.Infrastructure`, `StudioB2B.Domain`, `StudioB2B.Shared`
- [ ] Создать `StudioB2B.Tests.Integration` (SDK `Microsoft.NET.Sdk`, net10.0)
  - Пакеты: всё выше + `Testcontainers.MySql`, `WireMock.Net`, `Microsoft.EntityFrameworkCore.Design`
  - Ссылка на `StudioB2B.Infrastructure`
- [ ] Добавить оба проекта в `StudioB2B.sln`
- [ ] Создать `Directory.Build.props` с общими настройками тестовых проектов

## Фаза 2 — Unit-тесты бизнес-логики

**Цель:** покрыть детерминированную логику без зависимостей на IO.

### Задачи

- [ ] `CalculationEngineTests` — `Services/CalculationEngineTests.cs`
  - Базовое вычисление формулы (`Price * 0.1`)
  - Цепочка правил (результат правила 1 используется в формуле правила 2)
  - Деление на ноль → `decimal.MinValue`, ошибка в `LastErrors`
  - Пустая формула → пропускается
  - `GetBaseVariableNames` возвращает корректные ключи
  - `EvaluateFormula` с произвольным контекстом
- [ ] `KeyEncryptionServiceTests` — round-trip encrypt→decrypt, разные строки
- [ ] `CommunicationPaymentCalculatorTests` — проверить расчёты из `CommunicationPaymentCalculator.cs`
- [ ] `ScheduleCronBuilderTests` — корректные CRON-выражения для всех типов расписания
- [ ] `DomainHelperTests` — вспомогательные методы из `DomainHelper.cs`

## Фаза 3 — Reflection-тесты системы прав

**Цель:** автоматически обнаруживать «мёртвые» роли и неполные маппинги.

### Задачи

- [ ] `EnumDescriptionTests` — каждое значение всех трёх enum имеет непустой `[Description]`
- [ ] `PageEnumCoverageTests` — каждая страница защищена `[Authorize]`
  - Каждое значение `PageEnum` присутствует хотя бы в одном `.razor`
    через `@attribute [Authorize(Roles = nameof(PageEnum.XXX))]`
- [ ] `FunctionEnumCoverageTests` — каждая функция используется в UI
  - Каждое значение `FunctionEnum` встречается в `.razor` или `.cs`
    в `IsInRole(nameof(FunctionEnum.XXX))` / `AuthorizeView Roles="@nameof(FunctionEnum.XXX)"`
- [ ] `FunctionEnumMapTests` — каждая функция есть в `FunctionPageMap`
  - Проверяется через reflection на приватное поле `TenantDatabaseInitializer`
- [ ] `PageColumnEnumCoverageTests` — каждая колонка используется в `Col(...)`
- [ ] `PageColumnEnumMapTests` — каждая колонка есть в `ColumnPageMap`
- [ ] `NavServiceRoleTests` — роли NavItem ссылаются на существующие PageEnum

## Фаза 4 — Интеграционные тесты БД

**Цель:** проверить работу с реальной MySQL через EF Core.

### Инфраструктура тестов

- `TenantDbContextFixture.SeedReferenceDataAsync()` — идемпотентный метод, создаёт общие справочники:
  `MarketplaceClientType`, `MarketplaceClientMode`, `MarketplaceClient`, два `OrderStatus` (from/to).
  Сохраняет Guid-ы в публичных свойствах: `DefaultClientTypeId`, `DefaultModeId`, `DefaultClientId`, `DefaultFromStatusId`, `DefaultToStatusId`.

- `DatabaseSeeder` — статический класс-фабрика для минимальных тестовых сущностей:
  `MarketplaceClient`, `Shipment`, `Order`, `OrderTransaction`, `CommunicationTask`, `TaskLog`,
  `Warehouse`, `WarehouseStock`, `Product`, `Manufacturer`, `PriceType`, `OzonReturn`.

### Задачи

- [ ] `TenantDbContextFixture` — базовая фикстура: поднимает Testcontainers MySQL, применяет миграции, возвращает `TenantDbContext`
- [ ] `SeedTests` — `Integration/Seed/SeedTests.cs`
  - После `SeedPagesColumnsAndFunctionsAsync` в БД есть строки для всех `PageEnum`, `FunctionEnum`, `PageColumnEnum`
  - Идемпотентность: повторный вызов не создаёт дубликатов
- [ ] `PermissionCrudTests` — создание, обновление, удаление (soft-delete) права
- [ ] `UserCrudTests` — создание пользователя, назначение прав
- [ ] `AuditLogTests` — `SaveChangesAsync` после изменения сущности генерирует `FieldAuditLog`
- [ ] `SoftDeleteFilterTests` — удалённые сущности не возвращаются стандартным запросом
- [ ] `CalculationRuleCrudTests` — CRUD правил расчёта
- [ ] `OrderStatusCrudTests` — CRUD статусов заказов
- [ ] `MarketplaceClientCrudTests` — 5 кейсов: `GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetClientOptionsAsync` с фильтром по `allowedIds`
- [ ] `PriceTypeCrudTests` — 5 кейсов: create/update/soft-delete; update системного типа → `false`; пагинация с фильтром
- [ ] `OrderTransactionCrudTests` — 4 кейса: create/update/soft-delete; `GetOrderTransactionsPagedAsync` исключает удалённые
- [ ] `CommunicationTaskCrudTests` — 4 кейса: create/read/soft-delete; добавление `CommunicationTaskLog` с навигацией; обновление статуса
- [ ] `WarehouseCrudTests` — 4 кейса: create/update/soft-delete; `WarehouseStock` с навигацией через `Include`
- [ ] `ProductCrudTests` — 4 кейса: create; связь с `Manufacturer`; soft-delete; `ProductAttributeValue` с навигацией
- [ ] `OzonReturnCrudTests` — 4 кейса: create; `GetReturnsCountsAsync` группирует по `Type`; `GetReturnsPageAsync` фильтрует по тексту; счётчик отмен с заказом

- [ ] `OrderTransactionApplyTests` — 4 кейса: E2E ApplyAsync через `OrderTransactionService` с реальным DB
- [ ] `OrderTransactionHistoryTests` — 3 кейса: пагинация DESC; Dynamic LINQ фильтр; skip/take
- [ ] `OrderSelectionInfoTests` — 3 кейса: единый статус; смешанные статусы; null статус
- [ ] `SyncJobTests` — 5 кейсов: CRUD расписания и истории; фильтр по JobType
- [ ] `BuildAuditValueResolverTests` — 3 кейса: резолвинг StatusId/MarketplaceClientId; пустые логи
- [ ] `PermissionQueryTests` — 4 кейса: GetPagesWithDetails; GetEntityOptions; GetAvailable; GetById

## Фаза 5 — Тесты внешних сервисов

**Цель:** проверить интеграцию с Ozon API и SMTP без реальных соединений.

### Задачи

- [ ] `OzonApiClientTests` — `Integration/Ozon/OzonApiClientTests.cs`
  - Использовать `WireMock.Net` как HTTP-сервер
  - Корректный запрос к `GetSellerInfoAsync` (заголовки `Client-Id`, `Api-Key`)
  - Retry при 429 (rate limit)
  - Обработка ошибки 401 → `OzonApiResultDto` с ошибкой
  - Десериализация ответа с датами в UTC

## Фаза 6 — Архитектурные тесты

**Цель:** защитить архитектурные ограничения, описанные в coding guidelines.

### Задачи

- [ ] Установить пакет `NetArchTest.Rules` в `Tests.Unit`
- [ ] `ArchitectureTests` — `Architecture/ArchitectureTests.cs`
  - Blazor-страницы не используют `ITenantDbContextFactory` напрямую
  - Blazor-страницы не используют `IMapper` напрямую
  - Service-слой не зависит от `StudioB2B.Web`
  - Feature-слой содержит только extension-методы (static классы)

## Дополнительные тесты (бэклог)

- [ ] `OrderSyncServiceTests` — sync корректно агрегирует `OrderSyncSummaryDto`
- [ ] `TenantDatabaseInitializerTests` — `MigrateAndSeedAsync` в интеграционном тесте
- [ ] `OrderTransactionEngineTests` — применение транзакции с расчётом полей
- [ ] `CommunicationTaskSyncTests` — синхронизация чатов/вопросов из мок-ответа Ozon
- [ ] `JwtTokenTests` — генерация токена содержит нужные role-claims
- [ ] `TenantBackupServiceTests` — backup через мок MinIO

## Инструменты и пакеты

| Назначение | Пакет |
|---|---|
| Test runner | xunit 2.x |
| Assertions | FluentAssertions |
| Mocking | Moq |
| DB контейнер | Testcontainers.MySql |
| HTTP mock | WireMock.Net |
| Architecture | NetArchTest.Rules |
| Coverage | coverlet.collector + ReportGenerator |

