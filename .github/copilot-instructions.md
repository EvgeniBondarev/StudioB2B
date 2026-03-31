# Code Style Instructions

## 1. No alignment spaces

Use a single space only. Do **not** add extra spaces anywhere to push symbols into columns. This applies **everywhere**: parameter lists, field declarations, constructor bodies, method bodies, object initialisers, expression-body members (`=>`), and switch expression arms.

**Wrong — parameter list:**
```csharp
public record OrderPageRequest(
    Guid?   ClientId       = null,
    Guid?   StatusId       = null,
    Guid?   SystemStatusId = null,
    bool    HasReturn      = false,
    int     Skip           = 0,
    int     Take           = 15);
```

**Correct — parameter list:**
```csharp
public record OrderPageRequest(
    Guid? ClientId = null,
    Guid? StatusId = null,
    Guid? SystemStatusId = null,
    bool HasReturn = false,
    int Skip = 0,
    int Take = 15);
```

**Wrong — assignments in method body:**
```csharp
_filterEntity     = null;
_filterChangeType = null;
_filterUser       = null;
_filterFrom       = null;
_filterTo         = null;
```

**Correct — assignments in method body:**
```csharp
_filterEntity = null;
_filterChangeType = null;
_filterUser = null;
_filterFrom = null;
_filterTo = null;
```

**Wrong — object initialiser:**
```csharp
new CalculationRule
{
    Id        = Guid.NewGuid(),
    Name      = _name,
    Formula   = _formula,
    IsActive  = true
}
```

**Correct — object initialiser:**
```csharp
new CalculationRule
{
    Id = Guid.NewGuid(),
    Name = _name,
    Formula = _formula,
    IsActive = true
}
```

**Wrong — expression-body members:**
```csharp
private async Task OnFilterChange(object? _)   => await RefreshGrid();
private async Task OnDateFilterChange(DateTime? _) => await RefreshGrid();
private void      SaveRow(PriceType p)         => _grid?.UpdateRow(p);
```

**Correct — expression-body members:**
```csharp
private async Task OnFilterChange(object? _) => await RefreshGrid();
private async Task OnDateFilterChange(DateTime? _) => await RefreshGrid();
private void SaveRow(PriceType p) => _grid?.UpdateRow(p);
```

**Wrong — switch expression arms:**
```csharp
var label = type switch
{
    CommunicationTaskType.Chat     => "Чат",
    CommunicationTaskType.Question => "Вопрос",
    CommunicationTaskType.Review   => "Отзыв",
    _                              => "—"
};
```

**Correct — switch expression arms:**
```csharp
var label = type switch
{
    CommunicationTaskType.Chat => "Чат",
    CommunicationTaskType.Question => "Вопрос",
    CommunicationTaskType.Review => "Отзыв",
    _ => "—"
};
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

---

## 9. Role & Permission System

### Overview

The project uses three enums to control access. All three live in `StudioB2B.Domain/Constants/`.

| Enum | Controls | JWT role claim source |
|---|---|---|
| `PageEnum` | Which pages a user can open | `nameof(PageEnum.*)` |
| `FunctionEnum` | Which actions a user can perform | `nameof(FunctionEnum.*)` |
| `PageColumnEnum` | Which grid columns a user can see | `nameof(PageColumnEnum.*)` |

The **enum member name** (not the int value, not the description) is used verbatim as the JWT role claim and must match exactly everywhere in the code.

Every enum value **must** have a `[Description("...")]` attribute — this text is shown to the user in the Permissions UI.

---

### How TenantDatabaseInitializer seeds the data

`SeedPagesColumnsAndFunctionsAsync` in `TenantDatabaseInitializer.cs` iterates all three enums and automatically creates or updates rows in the `Page`, `PageColumn`, and `Function` tables. **No manual SQL or migration data is needed.**

However, `PageColumnEnum` and `FunctionEnum` also require entries in two static maps in the same file:

- **`ColumnPageMap`** — maps each `PageColumnEnum` value to its parent `PageEnum`
- **`FunctionPageMap`** — maps each `FunctionEnum` value to its parent `PageEnum`

If you add an enum value without updating the map, the new item **will not be seeded** and will not appear in the Permissions UI.

---

### Adding a new page

**Step 1 — Add to `PageEnum`**

```csharp
// StudioB2B.Domain/Constants/PageEnum.cs
[Description("Название страницы для UI")]
NewPageView = 18,   // next sequential integer
```

**Step 2 — Add to `NavService.cs`**

```csharp
new NavItem { Path = "/new-page", Role = nameof(PageEnum.NewPageView) }
```

**Step 3 — Create the Razor page**

```razor
@page "/new-page"
@attribute [Authorize(Roles = nameof(PageEnum.NewPageView))]
```

---

### Adding a new function (action)

**Step 1 — Add to `FunctionEnum`**

```csharp
// StudioB2B.Domain/Constants/FunctionEnum.cs
[Description("Страница: действие")]
NewPageManage = 17,   // next sequential integer
```

**Step 2 — Add to `FunctionPageMap` in `TenantDatabaseInitializer.cs`**

```csharp
[FunctionEnum.NewPageManage] = PageEnum.NewPageView,
```

**Step 3 — Use in the Razor page**

Declarative (hide/show a button or section):

```razor
<AuthorizeView Roles="@nameof(FunctionEnum.NewPageManage)">
    <Authorized>
        <RadzenButton Text="Создать" Click="@OpenCreateDialog" />
    </Authorized>
</AuthorizeView>
```

Imperative (in a C# code block):

```csharp
_canManage = state.User.IsInRole("Admin") || state.User.IsInRole(nameof(FunctionEnum.NewPageManage));
```

---

### Adding a new grid column

**Step 1 — Add to `PageColumnEnum`**

```csharp
// StudioB2B.Domain/Constants/PageColumnEnum.cs
[Description("Страница: колонка «Название»")]
NewPageColName = 95,   // next sequential integer
```

**Step 2 — Add to `ColumnPageMap` in `TenantDatabaseInitializer.cs`**

```csharp
[PageColumnEnum.NewPageColName] = PageEnum.NewPageView,
```

**Step 3 — Use on the grid column**

```razor
<RadzenDataGridColumn ... Visible="@Col(nameof(PageColumnEnum.NewPageColName))">
```

---

### Always use `nameof` — never raw strings

**Wrong:**
```csharp
@attribute [Authorize(Roles = "OrdersView")]
<AuthorizeView Roles="UsersManage">
IsInRole("UsersManage")
Col("OrdersColProduct")
new NavItem { Role = "ReturnsView" }
```

**Correct:**
```csharp
@attribute [Authorize(Roles = nameof(PageEnum.OrdersView))]
<AuthorizeView Roles="@nameof(FunctionEnum.UsersManage)">
IsInRole(nameof(FunctionEnum.UsersManage))
Col(nameof(PageColumnEnum.OrdersColProduct))
new NavItem { Role = nameof(PageEnum.ReturnsView) }
```

Note: inside a Razor HTML attribute use `"@nameof(...)"` (with `@`); inside a C# code block use `nameof(...)` without quotes.

---

### The special "Admin" role

`"Admin"` is a built-in role that bypasses all permission checks. It is **not** in any enum. Always keep it as a plain string literal:

```csharp
// Correct — Admin is always a plain string
_canManage = state.User.IsInRole("Admin") || state.User.IsInRole(nameof(FunctionEnum.UsersManage));
```

---

### Complete checklist

#### New page
- [ ] `PageEnum` — new value with `[Description]`
- [ ] `NavService.cs` — `NavItem` with `Role = nameof(PageEnum.*)`
- [ ] `.razor` file — `@attribute [Authorize(Roles = nameof(PageEnum.*))]`

#### New function
- [ ] `FunctionEnum` — new value with `[Description]`
- [ ] `TenantDatabaseInitializer.cs → FunctionPageMap` — new entry
- [ ] Razor — `<AuthorizeView Roles="@nameof(FunctionEnum.*)">` and/or `IsInRole(nameof(FunctionEnum.*))`

#### New grid column
- [ ] `PageColumnEnum` — new value with `[Description]`
- [ ] `TenantDatabaseInitializer.cs → ColumnPageMap` — new entry
- [ ] Razor — `Visible="@Col(nameof(PageColumnEnum.*))"`
