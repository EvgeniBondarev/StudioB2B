using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Services;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features.Users;

public static class UserQueryExtensions
{
    public static IQueryable<TenantUser> OrderByLastName(this IQueryable<TenantUser> q)
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
                .Join(db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
                .ToListAsync(ct);
            result.Add(mapper.Map<UserListDto>(u) with { Roles = roles });
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
            .Join(db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(ct);
        return mapper.Map<UserListDto>(u) with { Roles = roles };
    }
}

public class GetAvailableRoles(ITenantDbContextFactory factory)
{
    public async Task<List<string>> HandleAsync(CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        return await db.Roles.AsNoTracking().OrderBy(r => r.Name).Select(r => r.Name).ToListAsync(ct);
    }
}

public class CreateUser(ITenantDbContextFactory factory, IMapper mapper)
{
    public async Task<(bool Success, string? Error)> HandleAsync(
        CreateUserRequest request, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return (false, "Пользователь с таким email уже существует");

        var user = mapper.Map<TenantUser>(request);
        user.Id = Guid.NewGuid();
        user.HashPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        db.Users.Add(user);

        foreach (var roleName in request.Roles)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
            if (role is not null)
                db.UserRoles.Add(new TenantUserRole { UserId = user.Id, RoleId = role.Id });
        }

        await db.SaveChangesAsync(ct);
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

        // Sync roles
        var currentRoles = await db.UserRoles.Where(ur => ur.UserId == id).ToListAsync(ct);
        db.UserRoles.RemoveRange(currentRoles);

        foreach (var roleName in request.Roles)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
            if (role is not null)
                db.UserRoles.Add(new TenantUserRole { UserId = id, RoleId = role.Id });
        }

        await db.SaveChangesAsync(ct);
        return (true, null);
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

        user.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }
}
