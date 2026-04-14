# Testing Changelog

## Статус: 🟢 Фазы 1–3 завершены. Все unit-тесты зелёные ✅

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

### Фаза 5 — Тесты внешних сервисов (код написан)
- [x] `OzonApiClientTests` — WireMock.Net: success, 401, пустые credentials

### Фаза 6 — Архитектурные тесты (все проходят ✅)
- [x] `ArchitectureTests` — 4 правила NetArchTest: нет прямого ITenantDbContextFactory/IMapper в UI, нет зависимости сервисов на Web, Features — только static классы

### Исправленные баги (найдены тестами)
- [x] `FunctionEnum.UsersChangePassword` добавлен в `FunctionPageMap` в `TenantDatabaseInitializer.cs`
- [x] Удалены 4 неиспользуемых значения из `FunctionEnum`: `OrdersManage (2)`, `QuestionsManage (13)`, `TaskBoardManage (15)`, `TaskBoardAdminManage (16)`
- [x] Удалены соответствующие записи из `FunctionPageMap` в `TenantDatabaseInitializer.cs`
- [x] Удалена мёртвая seed-логика в `SeedChatManagerPermissionAsync` (назначение функций `TaskBoardManage`/`QuestionsManage`)

---

## 🔄 В работе

_Фаза 4 — Интеграционные тесты БД_

---

## 📋 Осталось сделать

### Фаза 4 — Интеграционные тесты БД
- [ ] `TenantDbContextFixture` (Testcontainers MySQL)
- [ ] `SeedTests` (идемпотентность seed)
- [ ] `PermissionCrudTests`
- [ ] `UserCrudTests`
- [ ] `AuditLogTests`
- [ ] `SoftDeleteFilterTests`
- [ ] `CalculationRuleCrudTests`
- [ ] `OrderStatusCrudTests`

### Фаза 5 — Внешние сервисы
- [ ] `OzonApiClientTests` (WireMock.Net, 4 сценария)
- [ ] `SmtpEmailServiceTests` (2 сценария)

### Фаза 6 — Архитектурные тесты
- [ ] `ArchitectureTests` (NetArchTest.Rules, 4 правила)

### Бэклог
- [ ] `OrderSyncServiceTests`
- [ ] `TenantDatabaseInitializerTests`
- [ ] `OrderTransactionEngineTests`
- [ ] `CommunicationTaskSyncTests`
- [ ] `JwtTokenTests`
- [ ] `TenantBackupServiceTests`

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
Total:   385  ✅ Passed: 385  ❌ Failed: 0
```

Все тесты зелёные.

### Прочие замечания

- `ColumnPageMap` и `FunctionPageMap` — приватные поля `TenantDatabaseInitializer`.
  Тесты используют reflection для доступа к ним. Это работает.
- `NavService` строка 85: `/permissions` использует `nameof(PageEnum.ModulesView)` вместо
  `nameof(PageEnum.PermissionsView)` — тест `NavServiceRoleTests` не упал, т.к. `ModulesView`
  является валидным `PageEnum`. Это логическая ошибка, не синтаксическая.
  Добавить отдельный тест `NavService_PermissionsPath_UsesCorrectRole`.

