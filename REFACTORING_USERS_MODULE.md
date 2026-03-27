# Рефакторинг модуля Users - Отделение бизнес-логики от UI

## ✅ Выполнено

### 1. Созданы новые файлы

#### `StudioB2B.Infrastructure/Interfaces/IUserService.cs`
- Интерфейс сервиса для работы с пользователями
- Определяет методы: GetAllUsersAsync, GetUserByIdAsync, CreateUserAsync, UpdateUserAsync, DeleteUserAsync

#### `StudioB2B.Infrastructure/Services/UserService.cs`
- Реализация IUserService
- Инкапсулирует работу с БД через ITenantDbContextFactory
- Использует extension-методы из UserFeatures (GetUsersAsync, CreateUserAsync и т.д.)
- Полностью изолирует UI от прямого доступа к DbContext

### 2. Обновленные файлы

#### `StudioB2B.Infrastructure/DependencyInjection.cs`
- ✅ Добавлена регистрация `services.AddScoped<IUserService, UserService>()`
- Сервис зарегистрирован в секции "Business logic services"

#### `StudioB2B.Web/Controllers/UsersController.cs`
- ❌ **УДАЛЕНО**: `ITenantDbContextFactory`, `using var db = ...`, прямые EF запросы
- ✅ **ДОБАВЛЕНО**: `IUserService` через конструктор
- Методы теперь делегируют всю логику сервису:
  - `GetUsers()` → `UserService.GetAllUsersAsync()`
  - `GetUser(id)` → `UserService.GetUserByIdAsync(id)`
  - `CreateUser()` → `UserService.CreateUserAsync()`
  - `UpdateUser()` → `UserService.UpdateUserAsync()`
  - `DeleteUser()` → `UserService.DeleteUserAsync()`

#### `StudioB2B.Web/Components/Pages/TenantUsers.razor`
- ❌ **УДАЛЕНО**: `@inject ITenantDbContextFactory`, `@inject IMapper`, `using StudioB2B.Infrastructure.Features`
- ✅ **ДОБАВЛЕНО**: `@inject IUserService UserService`
- Методы обновлены:
  - `LoadAsync()` → использует `UserService.GetAllUsersAsync()`
  - `DeleteUser()` → использует `UserService.DeleteUserAsync()`

#### `StudioB2B.Web/Components/Common/UserEditDialog.razor`
- ❌ **УДАЛЕНО**: `@inject IMapper`, прямые вызовы `db.CreateUserAsync()` и `db.UpdateUserAsync()`
- ✅ **ДОБАВЛЕНО**: `@inject IUserService UserService`
- Методы обновлены:
  - `OnInitializedAsync()` → использует `UserService.GetUserByIdAsync()`
  - `HandleSubmit()` → использует `UserService.CreateUserAsync()` и `UserService.UpdateUserAsync()`
- ⚠️ **ПРИМЕЧАНИЕ**: Оставлен доступ к `TenantFactory` для получения списка прав (`GetAvailablePermissionsAsync`) - это нормально, т.к. это вспомогательные данные. В будущем можно создать PermissionService.

## 📊 Результаты

### До рефакторинга
```razor
// UI компонент имел прямой доступ к БД
@inject ITenantDbContextFactory TenantFactory
using var db = TenantFactory.CreateDbContext();
_users = await db.GetUsersAsync(Mapper, CancellationToken.None);
```

### После рефакторинга
```razor
// UI компонент использует сервис
@inject IUserService UserService
_users = await UserService.GetAllUsersAsync(CancellationToken.None);
```

## 🎯 Преимущества

1. **Разделение ответственности**: UI больше не знает о DbContext и EF Core
2. **Тестируемость**: Контроллеры и компоненты легко тестировать с mock IUserService
3. **Повторное использование**: Логика работы с пользователями централизована в одном месте
4. **Следование паттерну**: Используются extension-методы из Infrastructure.Features (как и требовалось)
5. **Постепенная миграция**: Модуль Users рефакторен, остальные модули можно мигрировать постепенно

## 🔄 Следующие шаги

Применить тот же паттерн к другим модулям:
- ✅ **Users** - завершено
- ⏳ **MarketplaceClients** - следующий кандидат
- ⏳ **Orders**
- ⏳ **Transactions**
- ⏳ **Permissions**
- И т.д.

## 📝 Пример создания нового сервиса (MarketplaceClientsService)

```csharp
// 1. Создать интерфейс
public interface IMarketplaceClientService
{
    Task<List<MarketplaceClientDto>> GetAllAsync(CancellationToken ct = default);
    Task<MarketplaceClientDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    // ... другие методы
}

// 2. Создать реализацию
public class MarketplaceClientService : IMarketplaceClientService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IMapper _mapper;

    public MarketplaceClientService(ITenantDbContextFactory dbContextFactory, IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _mapper = mapper;
    }

    public async Task<List<MarketplaceClientDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.MarketplaceClients.GetAllAsync(_mapper, ct);
    }
    // ... остальные методы
}

// 3. Зарегистрировать в DependencyInjection.cs
services.AddScoped<IMarketplaceClientService, MarketplaceClientService>();

// 4. Обновить контроллеры и компоненты
@inject IMarketplaceClientService MarketplaceClientService
```

## ⚠️ Важные замечания

- Extension-методы из Infrastructure.Features остаются неизменными
- Сервисы являются тонкой оберткой над extension-методами
- UI компоненты больше не работают с DbContext напрямую
- Mapper и DbContextFactory используются только внутри сервисов
- Для вспомогательных данных (например, списка прав в диалогах) допустимо оставить прямой доступ к БД до создания соответствующего сервиса

