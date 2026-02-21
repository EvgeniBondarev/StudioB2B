using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features.Users;


public static class UserQueryExtensions
{
    public static IQueryable<ApplicationUser> OrderByLastName(this IQueryable<ApplicationUser> q)
        => q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
}

public class GetUsers(ITenantDbContextFactory factory, IMapper mapper)
{
    public async Task<List<UserListDto>> HandleAsync(CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var users = await db.Users.AsNoTracking().OrderByLastName().ToListAsync(ct);
        var result = new List<UserListDto>(users.Count);
        foreach (var u in users)
        {
            var roles = await db.UserRoles.AsNoTracking()
                .Where(ur => ur.UserId == u.Id)
                .Join(db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
                .ToListAsync(ct);
            var dto = mapper.Map<UserListDto>(u) with { Roles = roles };
            result.Add(dto);
        }
        return result;
    }
}

public class GetUserById(ITenantDbContextFactory factory, IMapper mapper)
{
    public async Task<UserListDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return null;
        var roles = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == u.Id)
            .Join(db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
            .ToListAsync(ct);
        return mapper.Map<UserListDto>(u) with { Roles = roles };
    }
}

public class GetAvailableRoles(ITenantDbContextFactory factory)
{
    public async Task<List<string>> HandleAsync(CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        return await db.Roles.AsNoTracking().OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync(ct);
    }
}

public class CreateUser(ITenantDbContextFactory factory, IMapper mapper)
{
    public async Task<(bool Success, string? Error)> HandleAsync(
        CreateUserRequest request, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        using var um = UserManagerFactory.Create(db);
        var user = mapper.Map<ApplicationUser>(request);
        var result = await um.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        foreach (var role in request.Roles)
            if (await db.Roles.AnyAsync(r => r.Name == role, ct))
                await um.AddToRoleAsync(user, role);
        return (true, null);
    }
}

public class UpdateUser(ITenantDbContextFactory factory, IMapper mapper)
{
    public async Task<(bool Success, string? Error)> HandleAsync(
        Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return (false, "Пользователь не найден");

        mapper.Map(request, user);

        using var um = UserManagerFactory.Create(db);
        var current = await um.GetRolesAsync(user);
        var toRemove = current.Except(request.Roles).ToList();
        var toAdd = request.Roles.Except(current).ToList();
        if (toRemove.Count > 0) await um.RemoveFromRolesAsync(user, toRemove);
        foreach (var role in toAdd)
            if (await db.Roles.AnyAsync(r => r.Name == role, ct))
                await um.AddToRoleAsync(user, role);
        var result = await um.UpdateAsync(user);
        return result.Succeeded
            ? (true, null)
            : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}


public class DeleteUser(ITenantDbContextFactory factory)
{
    public async Task<(bool Success, string? Error)> HandleAsync(
        Guid id, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return (false, "Пользователь не найден");
        using var um = UserManagerFactory.Create(db);
        var result = await um.DeleteAsync(user);
        return result.Succeeded
            ? (true, null)
            : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}

internal static class UserManagerFactory
{
    internal static UserManager<ApplicationUser> Create(TenantDbContext db)
    {
        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore
            .UserStore<ApplicationUser, ApplicationRole, TenantDbContext, Guid>(db);
        return new UserManager<ApplicationUser>(
            store,
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()],
            [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}


