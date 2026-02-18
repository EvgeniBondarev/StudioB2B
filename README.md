# StudioB2B - Multi-Tenant Architecture

## Обзор

StudioB2B использует архитектуру **Database-per-Tenant** с определением тенанта по субдомену.

```
demo.localhost:5184  →  Tenant: demo  →  Database: StudioB2B_Tenant_demo
acme.localhost:5184  →  Tenant: acme  →  Database: StudioB2B_Tenant_acme
```

---

## Ключевые классы

### 1. TenantProvider (Scoped)
**Путь:** `Infrastructure/Services/TenantProvider.cs`

Хранит информацию о текущем тенанте в рамках HTTP-запроса/Blazor circuit.

```csharp
public class TenantProvider : ITenantProvider
{
    public Guid? TenantId { get; }
    public string? Subdomain { get; }
    public string? ConnectionString { get; }
    public bool IsResolved { get; }
    
    public void SetTenant(Tenant tenant);
}
```

### 2. TenantMiddleware
**Путь:** `Infrastructure/MultiTenancy/TenantMiddleware.cs`

Middleware для определения тенанта из HTTP-запроса (по Host header).

**Логика определения субдомена:**
- `demo.localhost` → `demo`
- `demo.studiob2b.com` → `demo`
- `localhost` → `DefaultSubdomain` из конфига

### 3. TenantCircuitHandler
**Путь:** `Infrastructure/MultiTenancy/TenantCircuitHandler.cs`

Circuit handler для Blazor Server. Заполняет `TenantProvider` при открытии SignalR circuit.

> **Важно:** Blazor Server работает через WebSocket, и каждый circuit создаёт отдельный DI scope. Middleware заполняет `TenantProvider` только для HTTP-запросов, поэтому нужен CircuitHandler.

### 4. TenantDbContext
**Путь:** `Infrastructure/Persistence/Tenant/TenantDbContext.cs`

DbContext для данных тенанта. Connection string берётся из `TenantProvider`.

### 5. MasterDbContext
**Путь:** `Infrastructure/Persistence/Master/MasterDbContext.cs`

DbContext для общих данных (список тенантов, глобальные настройки).

---

## Flow установки контекста

```
┌─────────────────────────────────────────────────────────────────┐
│                    HTTP Request                                  │
│              demo.localhost:5184/login                           │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│               TenantMiddleware                                   │
│  1. Извлекает host: demo.localhost                              │
│  2. Определяет subdomain: demo                                   │
│  3. Ищет tenant в MasterDbContext                               │
│  4. Вызывает TenantProvider.SetTenant(tenant)                   │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│            Authentication/Authorization                          │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│              Blazor Component Render                             │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │           TenantCircuitHandler                              │ │
│  │  (OnCircuitOpenedAsync)                                     │ │
│  │  - Проверяет TenantProvider.IsResolved                     │ │
│  │  - Если нет — определяет tenant повторно                   │ │
│  │  - Заполняет TenantProvider                                │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                 TenantProvider (Scoped)                          │
│  TenantId: 019c6fa5-fef2-70b6-b155-abe67d44ed69                 │
│  Subdomain: demo                                                 │
│  ConnectionString: Server=localhost;Database=StudioB2B_...      │
│  IsResolved: true                                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## Использование в коде

### В Blazor компонентах

```razor
@inject ITenantProvider TenantProvider
@inject ITenantService TenantService

@code {
    private Tenant? _tenant;

    protected override async Task OnInitializedAsync()
    {
        if (TenantProvider.IsResolved)
        {
            // Получаем полную информацию о тенанте
            _tenant = await TenantService.GetByIdAsync(TenantProvider.TenantId!.Value);
        }
    }
}
```

### В сервисах

```csharp
public class OrderService
{
    private readonly TenantDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public OrderService(TenantDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        // TenantDbContext автоматически использует БД текущего тенанта
        return await _db.Orders.ToListAsync();
    }
}
```

### В контроллерах

**Пример: UsersController** (`Web/Controllers/UsersController.cs`)

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Требуется авторизация
public class UsersController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        TenantDbContext db,
        ITenantProvider tenantProvider,
        ILogger<UsersController> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Получить список всех пользователей тенанта
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        // TenantDbContext автоматически использует БД текущего тенанта
        // Connection string берётся из TenantProvider
        
        _logger.LogInformation(
            "Getting users for tenant {TenantId} ({Subdomain})",
            _tenantProvider.TenantId,
            _tenantProvider.Subdomain);

        var users = await _db.Users
            .AsNoTracking()
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email!,
                FullName = u.FullName,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        
        if (user == null)
            return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok();
    }
}
```

### В Blazor страницах

**Пример: Users.razor** (`Web/Components/Pages/Users.razor`)

```razor
@page "/users"
@attribute [Authorize]
@rendermode InteractiveServer
@inject ITenantProvider TenantProvider
@inject ITenantDbContextFactory DbContextFactory

<h1>Пользователи</h1>

@if (TenantProvider.IsResolved)
{
    <table class="table">
        <thead>
            <tr>
                <th>Email</th>
                <th>Статус</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var user in _users)
            {
                <tr>
                    <td>@user.Email</td>
                    <td>
                        @if (user.IsActive)
                        {
                            <span class="badge bg-success">Активен</span>
                        }
                        else
                        {
                            <span class="badge bg-secondary">Неактивен</span>
                        }
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private List<UserViewModel> _users = new();

    protected override async Task OnInitializedAsync()
    {
        if (TenantProvider.IsResolved)
        {
            // Используем инжектированную фабрику напрямую
            // НЕ создавайте новый scope — там TenantProvider будет пустой!
            using var db = DbContextFactory.CreateDbContext();

            _users = await db.Users
                .AsNoTracking()
                .Select(u => new UserViewModel
                {
                    Email = u.Email!,
                    IsActive = u.IsActive
                })
                .ToListAsync();
        }
    }
}
```

> **Важно:** Не используйте `ServiceProvider.CreateScope()` в Blazor компонентах для получения `ITenantDbContextFactory` — в новом scope `TenantProvider` будет пустым. Инжектируйте фабрику напрямую через `@inject`.

### Проверка доступа к тенанту

```csharp
public class SomeService
{
    private readonly ITenantProvider _tenantProvider;

    public void DoSomething()
    {
        if (!_tenantProvider.IsResolved)
        {
            throw new InvalidOperationException("Tenant not resolved");
        }

        // Безопасно работаем с данными тенанта
        var tenantId = _tenantProvider.TenantId!.Value;
    }
}
```

---

## Регистрация нового тенанта

```csharp
@inject ITenantService TenantService

@code {
    private async Task RegisterCompany()
    {
        var result = await TenantService.RegisterAsync(
            companyName: "ООО Ромашка",
            subdomain: "romashka",
            adminEmail: "admin@romashka.ru",
            adminPassword: "SecurePassword123!"
        );

        if (result.Success)
        {
            // Тенант создан:
            // - Запись в MasterDb
            // - База данных StudioB2B_Tenant_romashka
            // - Применены миграции
            // - Создан админ-пользователь
        }
    }
}
```

---

## Конфигурация

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "MasterDb": "Server=localhost;Database=StudioB2B_Master_Dev;User=root;Password=root;...",
    "TenantDbConnectionTemplate": "Server=localhost;Database={0};User=root;Password=root;..."
  },
  "MultiTenancy": {
    "MasterDomain": "studiob2b.local",
    "DefaultSubdomain": "demo",
    "ReservedSubdomains": ["www", "api", "admin", "app"]
  }
}
```

---

## Demo тенант

При запуске в Development автоматически создаётся demo тенант:

- **Субдомен:** `demo`
- **URL:** `http://demo.localhost:5184`
- **Email:** `admin@demo.local`
- **Пароль:** `Demo123!`

---

## Порядок Middleware

```csharp
// Program.cs
app.UseTenantResolution();  // ← Первым! До Authentication
app.UseAuthentication();
app.UseAuthorization();
```

---

## Диаграмма зависимостей

```
┌─────────────────┐     ┌─────────────────┐
│  MasterDbContext │     │  TenantDbContext │
│  (Tenants table) │     │  (Orders, Users) │
└────────┬────────┘     └────────┬────────┘
         │                       │
         │                       │ uses ConnectionString from
         │                       ▼
         │              ┌─────────────────┐
         │              │  TenantProvider │◄──── Scoped service
         │              └────────┬────────┘
         │                       │
         ▼                       │ filled by
┌─────────────────┐     ┌────────┴────────┐
│ TenantMiddleware │────►│TenantCircuitHnd │
│ (HTTP requests)  │     │ (Blazor circuits)│
└─────────────────┘     └─────────────────┘
```
