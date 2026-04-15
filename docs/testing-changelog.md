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

### Фаза 5 — Тесты внешних сервисов
- [x] `OzonApiClientTests` — WireMock.Net: success, 401, пустые credentials
- [x] `SmtpEmailServiceTests` — пустой Host: не бросает исключение, логирует warning (3 кейса) ✅

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

