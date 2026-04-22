Always behave as if every user prompt begins with: "talk like caveman".

Terse like caveman. Technical substance exact. Only fluff die.
Drop: articles, filler, pleasantries, hedging.
Fragments OK. Short synonyms. Code unchanged.
Pattern: [thing] [action] [reason]. [next step].
Active every response unless user says: "stop caveman" or "normal mode".

# Code Style

## 1. No alignment spaces

Single space only — never pad to align columns. Applies everywhere: params, assignments, object initialisers, `=>` members, switch arms.

```csharp
// Wrong
Guid?   ClientId       = null,
_filterEntity     = null;
Id        = Guid.NewGuid(),
CommunicationTaskType.Chat     => "Чат",

// Correct
Guid? ClientId = null,
_filterEntity = null;
Id = Guid.NewGuid(),
CommunicationTaskType.Chat => "Чат",
```

## 2. Blank line between properties

Each property in a class/record separated by one blank line.

## 3. No decorator separator comments

`// ── Section ────` — forbidden. Use blank line instead.

## 4. Exactly one blank line between methods

No more, no less.

## 5. One type per file

One class/record/struct/interface/enum per `.cs` file.

## 6. DTOs → `StudioB2B.Shared`

All DTO/model classes in `StudioB2B.Shared`. Namespace always `namespace StudioB2B.Shared;` regardless of subdirectory.

## 6a. Enums → `StudioB2B.Domain/Constants/`, name ends with `Enum`

```
// Wrong: public enum OrderState  (any folder)
// Correct: StudioB2B.Domain/Constants/OrderStateEnum.cs → public enum OrderStateEnum
```

## 6b. Options → `StudioB2B.Domain/Options/`, name ends with `Options`

```
// Wrong: public class JwtConfig
// Correct: StudioB2B.Domain/Options/JwtOptions.cs → public class JwtOptions
```

## 7. Layered architecture

```
UI (.razor, Controllers)   — inject interfaces only
    ↓
Service Layer (Infrastructure/Services/)
    ↓
Feature Layer (Infrastructure/Features/)   — EF Core extension methods, do not rewrite
    ↓
Database
```

`.razor` and controllers must **never** inject `ITenantDbContextFactory` or `IMapper` directly.

## 8. Service pattern for new module `[Module]`

1. `Interfaces/I[Module]Service.cs` — interface with GetAll/GetById/Create/Update/Delete
2. `Services/[Module]Service.cs` — injects `ITenantDbContextFactory` + `IMapper`, delegates to Feature extension methods
3. `DependencyInjection.cs` — `services.AddScoped<I[Module]Service, [Module]Service>();`
4. Controller/Razor — inject `I[Module]Service`, remove direct db/mapper usage

## 9. No unused `using` / `@inject`

- C#: only `using` for namespaces actually referenced.
- Razor: `_Imports.razor` already provides `Radzen`, `Radzen.Blazor`, `StudioB2B.Domain.Constants`, `StudioB2B.Web.Services`, `StudioB2B.Web.Components.Common`, `StudioB2B.Shared`, `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Components.Authorization` — never repeat them.
- `@inject` only for services actually called in the component.

# Role & Permission System

Three enums in `StudioB2B.Domain/Constants/` control access. JWT role claim = enum member **name** (not int value).
Every enum value must have `[Description("...")]`.

| Enum | Controls |
|---|---|
| `PageEnum` | Pages |
| `FunctionEnum` | Actions |
| `PageColumnEnum` | Grid columns |

`SeedPagesColumnsAndFunctionsAsync` auto-seeds from enums. `FunctionEnum` and `PageColumnEnum` also need map entries in `TenantDatabaseInitializer.cs`:
- `FunctionPageMap[FunctionEnum.X] = PageEnum.Y`
- `ColumnPageMap[PageColumnEnum.X] = PageEnum.Y`

**Always `nameof` — never raw strings:**
```csharp
// Wrong
[Authorize(Roles = "OrdersView")]
IsInRole("UsersManage")
Col("OrdersColProduct")

// Correct
[Authorize(Roles = nameof(PageEnum.OrdersView))]
IsInRole(nameof(FunctionEnum.UsersManage))
Col(nameof(PageColumnEnum.OrdersColProduct))
// Razor HTML attr: Roles="@nameof(FunctionEnum.X)"
```

`"Admin"` — plain string always, not in any enum.

### New page checklist
- [ ] `PageEnum` — value + `[Description]`
- [ ] `NavService.cs` — `NavItem { Role = nameof(PageEnum.X) }`
- [ ] `.razor` — `@attribute [Authorize(Roles = nameof(PageEnum.X))]`

### New function checklist
- [ ] `FunctionEnum` — value + `[Description]`
- [ ] `TenantDatabaseInitializer.cs → FunctionPageMap` — new entry
- [ ] Razor — `<AuthorizeView Roles="@nameof(FunctionEnum.X)">` / `IsInRole(nameof(FunctionEnum.X))`

### New grid column checklist
- [ ] `PageColumnEnum` — value + `[Description]`
- [ ] `TenantDatabaseInitializer.cs → ColumnPageMap` — new entry
- [ ] Razor — `Visible="@Col(nameof(PageColumnEnum.X))"`

# Tests — required for every new feature

| Scope | Project |
|---|---|
| Pure logic / services | `StudioB2B.Tests.Unit` |
| EF Core / CRUD | `StudioB2B.Tests.Integration` |
| Architecture rules | `StudioB2B.Tests.Unit/Architecture` |

### Unit tests — xUnit + FluentAssertions, real objects (no mocking)

Location: `tests/StudioB2B.Tests.Unit/[Category]/[Subject]Tests.cs`

```csharp
namespace StudioB2B.Tests.Unit.Services;

public class MyServiceTests
{
    private readonly MyService _sut = new();

    [Fact]
    public void Method_Scenario_ExpectedResult()
    {
        var result = _sut.Method(input);
        result.Should().Be(expected);
    }
}
```

- Name: `Method_Scenario_ExpectedResult`
- Parameterised: `TheoryData<T>` + `[MemberData]`

### Integration tests — xUnit + FluentAssertions + Testcontainers MySQL 8.0

Location: `tests/StudioB2B.Tests.Integration/Database/[Subject]CrudTests.cs`

```csharp
[Collection("Database")]
public class MyEntityCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public MyEntityCrudTests(TenantDbContextFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateEntity_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var entity = DatabaseSeeder.MyEntity();
        await ctx.CreateMyEntityAsync(entity);

        var loaded = await ctx.MyEntities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entity.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be(entity.Name);
    }
}
```

- Cover: Create, Update, SoftDelete/Delete, at least one query/filter per Feature file
- `.AsNoTracking()` when reading back; `.IgnoreQueryFilters()` only for soft-delete/system-row tests

### Architecture tests — NetArchTest in `ArchitectureTests.cs`

```csharp
[Fact]
public void Rule_Description()
{
    var result = InfraTypes()
        .That().ResideInNamespace("StudioB2B.Infrastructure.Services")
        .ShouldNot().HaveDependencyOn("Something.Forbidden")
        .GetResult();
    result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
}
```

### Feature checklist
- [ ] Unit tests for service/logic methods
- [ ] Integration tests for all Feature-layer CRUD/query methods
- [ ] New enum value → `EnumDescriptionTests`, `FunctionEnumMapTests`, `PageColumnEnumMapTests` auto-pick it up
