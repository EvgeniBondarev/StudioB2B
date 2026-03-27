# 🚀 БЫСТРЫЙ СТАРТ - Рефакторинг следующего модуля

## Шаблон для копирования (MarketplaceClients)

### Шаг 1: Создать интерфейс (5 минут)

**Файл:** `StudioB2B.Infrastructure/Interfaces/IMarketplaceClientService.cs`

```csharp
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IMarketplaceClientService
{
    Task<List<MarketplaceClientDto>> GetAllAsync(CancellationToken ct = default);
    Task<MarketplaceClientDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MarketplaceClientDto> CreateAsync(CreateMarketplaceClientDto dto, CancellationToken ct = default);
    Task<MarketplaceClientDto?> UpdateAsync(UpdateMarketplaceClientDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

---

### Шаг 2: Создать реализацию (10 минут)

**Файл:** `StudioB2B.Infrastructure/Services/MarketplaceClientService.cs`

```csharp
using AutoMapper;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Services;

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
        return await db.MarketplaceClients!.GetAllAsync(_mapper, ct);
    }

    public async Task<MarketplaceClientDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.MarketplaceClients!.GetByIdAsync(id, _mapper, ct);
    }

    public async Task<MarketplaceClientDto> CreateAsync(CreateMarketplaceClientDto dto, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.CreateAsync(dto, _mapper, ct);
    }

    public async Task<MarketplaceClientDto?> UpdateAsync(UpdateMarketplaceClientDto dto, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.UpdateAsync(dto, _mapper, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.DeleteAsync(id, ct);
    }
}
```

---

### Шаг 3: Зарегистрировать в DI (1 минута)

**Файл:** `StudioB2B.Infrastructure/DependencyInjection.cs`

Найти секцию:
```csharp
// Business logic services (wrapping Features for UI layer)
services.AddScoped<IUserService, UserService>();
```

Добавить после:
```csharp
services.AddScoped<IMarketplaceClientService, MarketplaceClientService>();
```

---

### Шаг 4: Обновить контроллер (5 минут)

**Файл:** `StudioB2B.Web/Controllers/MarketplaceClientsController.cs`

#### Было:
```csharp
private readonly ITenantDbContextFactory _dbContextFactory;
private readonly IMapper _mapper;

public MarketplaceClientsController(ITenantDbContextFactory dbContextFactory, IMapper mapper)
{
    _dbContextFactory = dbContextFactory;
    _mapper = mapper;
}

[HttpGet]
public async Task<IActionResult> GetAll()
{
    using var db = _dbContextFactory.CreateDbContext();
    var list = await db.MarketplaceClients!.GetAllAsync(_mapper);
    return Ok(list);
}
```

#### Стало:
```csharp
private readonly IMarketplaceClientService _marketplaceClientService;

public MarketplaceClientsController(IMarketplaceClientService marketplaceClientService)
{
    _marketplaceClientService = marketplaceClientService;
}

[HttpGet]
public async Task<IActionResult> GetAll()
{
    var list = await _marketplaceClientService.GetAllAsync();
    return Ok(list);
}
```

**Аналогично обновить остальные методы контроллера.**

---

### Шаг 5: Обновить Razor компонент (10 минут)

**Файл:** `StudioB2B.Web/Components/Pages/MarketplaceClients.razor`

#### Было:
```razor
@using StudioB2B.Infrastructure.Features
@inject ITenantDbContextFactory Factory
@inject IMapper Mapper

private async Task LoadData(LoadDataArgs args)
{
    using var db = _db ?? Factory.CreateDbContext();
    var query = db.MarketplaceClients!.IncludeEverything().AsNoTracking();
    // ... работа с query
}
```

#### Стало:
```razor
@inject IMarketplaceClientService MarketplaceClientService

private async Task LoadData(LoadDataArgs args)
{
    // Для простых случаев:
    _pageItems = await MarketplaceClientService.GetAllAsync();
    
    // Для сложных случаев с фильтрацией/сортировкой:
    // Можно добавить методы в IMarketplaceClientService:
    // Task<(List<MarketplaceClientDto> Items, int Total)> GetPagedAsync(...)
}
```

**ВНИМАНИЕ:** MarketplaceClients.razor использует сложную логику с LoadDataArgs, фильтрацией и пагинацией. 
Возможно, потребуется добавить дополнительные методы в IMarketplaceClientService или оставить часть логики.

---

## 📋 Чек-лист для каждого модуля

- [ ] Создать `I[Module]Service.cs` в Infrastructure/Interfaces/
- [ ] Создать `[Module]Service.cs` в Infrastructure/Services/
- [ ] Добавить регистрацию в `DependencyInjection.cs`
- [ ] Обновить контроллер (если есть)
- [ ] Обновить Razor компонент(ы)
- [ ] Обновить диалоги (если есть)
- [ ] Запустить `dotnet build`
- [ ] Проверить отсутствие ошибок компиляции

---

## 🎯 Модули по приоритету

1. ✅ **Users** - завершено
2. ⏳ **MarketplaceClients** - следующий
3. ⏳ **Permissions** - простой модуль
4. ⏳ **OrderStatuses** - простой модуль
5. ⏳ **PriceTypes** - простой модуль
6. ⏳ **Orders** - сложный модуль
7. ✅ **Transactions** - завершено
8. ⏳ **CalculationRules** - сложный модуль

---

## ⚡ Быстрые команды

```bash
# Компиляция Infrastructure
cd /Users/korol/Documents/files/StudioB2B/StudioB2B
dotnet build StudioB2B.Infrastructure/StudioB2B.Infrastructure.csproj

# Компиляция Web
dotnet build StudioB2B.Web/StudioB2B.Web.csproj

# Компиляция всего решения
dotnet build

# Проверка ошибок
dotnet build 2>&1 | grep -i error
```

---

## 💡 Советы

1. **Начните с простых модулей** (Permissions, OrderStatuses) для тренировки
2. **Копируйте UserService** как шаблон
3. **Не меняйте extension-методы** - они остаются как есть
4. **Commit после каждого модуля** - для возможности отката
5. **Тестируйте компиляцию** после каждого шага

---

## 🚨 Частые ошибки

1. **Забыть зарегистрировать сервис** в DependencyInjection.cs
2. **Не добавить using** для интерфейса в Razor компоненте
3. **Оставить старые injections** (ITenantDbContextFactory, IMapper)
4. **Не обновить все места** использования модуля

---

## 📞 Что делать при проблемах

1. Проверить регистрацию в DI
2. Проверить компиляцию Infrastructure проекта отдельно
3. Посмотреть на UserService как эталон
4. Проверить using директивы в Razor файлах
5. Запустить `dotnet clean && dotnet build`

---

**Готовы начать следующий модуль?** 🚀

