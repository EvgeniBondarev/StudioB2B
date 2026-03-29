# Code Style Instructions

## 1. No alignment spaces

Use a single space only. Do **not** add extra spaces to align `=` signs, types, or identifiers in columns.

**Wrong:**
```csharp
public record OrderPageRequest(
    Guid?   ClientId       = null,
    Guid?   StatusId       = null,
    Guid?   SystemStatusId = null,
    Guid?   WarehouseId    = null,
    bool    HasReturn      = false,
    string? SchemeType     = null,
    string? SearchText     = null,
    string? DynamicFilter  = null,
    string? OrderBy        = null,
    int     Skip           = 0,
    int     Take           = 15,
    bool    FetchAll       = false);
```

**Correct:**
```csharp
public record OrderPageRequest(
    Guid? ClientId = null,
    Guid? StatusId = null,
    Guid? SystemStatusId = null,
    Guid? WarehouseId = null,
    bool HasReturn = false,
    string? SchemeType = null,
    string? SearchText = null,
    string? DynamicFilter = null,
    string? OrderBy = null,
    int Skip = 0,
    int Take = 15,
    bool FetchAll = false);
```

---

## 2. Blank line between properties in models

Each property in a class or record must be separated from the next by a blank line.

**Wrong:**
```csharp
public class ReturnsSyncResultDto
{
    public int Created { get; set; }
    public int Updated { get; set; }
    /// <summary>Отправлений, которым проставлен HasReturn = true.</summary>
    public int Linked { get; set; }
}
```

**Correct:**
```csharp
public class ReturnsSyncResultDto
{
    public int Created { get; set; }

    public int Updated { get; set; }

    /// <summary>Отправлений, которым проставлен HasReturn = true.</summary>
    public int Linked { get; set; }
}
```

---

## 3. No separator comments

Do **not** use decorative separator comments to divide sections of code.

**Wrong:**
```csharp
// ── Label helpers ──────────────────────────────────────────────────────
```

**Correct:** Use no separator at all, or use a single blank line to visually group related members.

---

## 4. Exactly one blank line between methods

Methods must be separated by **exactly one** blank line — no more.

**Wrong:**
```csharp
private void TogglePage(string name)
{
    if (_model.Pages.Contains(name)) _model.Pages.Remove(name);
    else _model.Pages.Add(name);
}


private static void ToggleString(List<string> list, string value)
{
    if (list.Contains(value)) list.Remove(value);
    else list.Add(value);
}
```

**Correct:**
```csharp
private void TogglePage(string name)
{
    if (_model.Pages.Contains(name)) _model.Pages.Remove(name);
    else _model.Pages.Add(name);
}

private static void ToggleString(List<string> list, string value)
{
    if (list.Contains(value)) list.Remove(value);
    else list.Add(value);
}
```

---

## 5. One class per file

Each `.cs` file must contain **exactly one** top-level type (class, record, struct, interface, or enum).

**Wrong:**
```csharp
// OrderDtos.cs
public class CreateOrderDto { ... }
public class UpdateOrderDto { ... }
public class OrderDto      { ... }
```

**Correct:**
```
CreateOrderDto.cs  → public class CreateOrderDto { ... }
UpdateOrderDto.cs  → public class UpdateOrderDto { ... }
OrderDto.cs        → public class OrderDto       { ... }
```

---

## 6. All models belong to the `StudioB2B.Shared` project

- All DTO and model classes must be placed in the **`StudioB2B.Shared`** project.
- Subdirectories within `StudioB2B.Shared` are used for file organisation only and must **not** introduce additional namespace levels.
- Every file in `StudioB2B.Shared`, regardless of which subdirectory it resides in, must declare the root project namespace:

```csharp
namespace StudioB2B.Shared;
```

---

## 6. Layered architecture

The project is divided into four layers. Each layer must only communicate with the layer directly below it.

```
UI Layer  (Blazor .razor, Controllers)
    ↓  @inject / constructor — interfaces only
Service Layer  (Infrastructure/Services/)
    ↓  uses extension-methods
Feature Layer  (Infrastructure/Features/)
    ↓  EF Core
Database
```

- UI components (`.razor`, controllers) must **never** inject `ITenantDbContextFactory` or `IMapper` directly.
- All database work goes through the Service layer.

---

## 7. Service pattern — adding a new module

For every new module `[Module]` follow these steps:

**Step 1 — Interface** `StudioB2B.Infrastructure/Interfaces/I[Module]Service.cs`

```csharp
namespace StudioB2B.Infrastructure.Interfaces;

public interface I[Module]Service
{
    Task<List<[Module]Dto>> GetAllAsync(CancellationToken ct = default);
    Task<[Module]Dto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<[Module]Dto> CreateAsync(Create[Module]Dto dto, CancellationToken ct = default);
    Task<[Module]Dto?> UpdateAsync(Update[Module]Dto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

**Step 2 — Implementation** `StudioB2B.Infrastructure/Services/[Module]Service.cs`

```csharp
namespace StudioB2B.Infrastructure.Services;

public class [Module]Service : I[Module]Service
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IMapper _mapper;

    public [Module]Service(ITenantDbContextFactory dbContextFactory, IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _mapper = mapper;
    }

    public async Task<List<[Module]Dto>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.[Module]s.GetAllAsync(_mapper, ct);
    }

    // ... remaining methods follow the same pattern
}
```

**Step 3 — DI registration** in `StudioB2B.Infrastructure/DependencyInjection.cs`

```csharp
services.AddScoped<I[Module]Service, [Module]Service>();
```

**Step 4 — Update controller**

```csharp
// Remove: ITenantDbContextFactory, IMapper, direct db.* calls
// Add:
private readonly I[Module]Service _[module]Service;
```

**Step 5 — Update Razor component**

```razor
@* Remove: @inject ITenantDbContextFactory, @inject IMapper *@
@inject I[Module]Service [Module]Service
```

---

## 8. Extension methods in Features stay unchanged

Files in `StudioB2B.Infrastructure/Features/` contain extension methods on `TenantDbContext` that perform actual database queries. **Do not move or rewrite them.** Services are thin wrappers that delegate to these extension methods.

```
[Module]Service.GetAllAsync()
    → using var db = _dbContextFactory.CreateDbContext()
    → db.[Module]s.GetAllAsync(_mapper, ct)   // extension method in [Module]Features.cs
```

