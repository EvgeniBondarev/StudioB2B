using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Features;

public static class UserExtensions
{
    public static IQueryable<TenantUser> OrderByLastName(this IQueryable<TenantUser> q)
        => q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);

    public static async Task<List<UserListDto>> GetUsersAsync(this TenantDbContext db, IMapper mapper, CancellationToken ct = default)
    {
        var users = await db.Users.AsNoTracking().OrderByLastName().ToListAsync(ct);
        var result = new List<UserListDto>(users.Count);
        foreach (var u in users)
        {
            var permissions = await db.UserPermissions.AsNoTracking()
                .Where(up => up.UserId == u.Id)
                .Join(db.Permissions.AsNoTracking(), up => up.PermissionId, p => p.Id, (_, p) => p.Name)
                .ToListAsync(ct);
            result.Add(mapper.Map<UserListDto>(u) with { Permissions = permissions });
        }
        return result;
    }

    public static async Task<UserListDto?> GetUserByIdAsync(this TenantDbContext db, Guid id, IMapper mapper, CancellationToken ct = default)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return null;
        var permissions = await db.UserPermissions.AsNoTracking()
            .Where(up => up.UserId == u.Id)
            .Join(db.Permissions.AsNoTracking(), up => up.PermissionId, p => p.Id, (_, p) => p.Name)
            .ToListAsync(ct);
        return mapper.Map<UserListDto>(u) with { Permissions = permissions };
    }

    public static async Task<(bool Success, string? Error)> CreateUserAsync(this TenantDbContext db, CreateUserDto request, IMapper mapper, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return (false, "Пользователь с таким email уже существует");

        var user = mapper.Map<TenantUser>(request);
        user.Id = Guid.NewGuid();
        user.HashPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        db.Users.Add(user);

        foreach (var permId in request.Permissions)
        {
            if (await db.Permissions.AnyAsync(p => p.Id == permId && !p.IsDeleted, ct))
                db.UserPermissions.Add(new TenantUserPermission { UserId = user.Id, PermissionId = permId });
        }

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public static async Task<(bool Success, string? Error)> UpdateUserAsync(this TenantDbContext db, Guid id, UpdateUserDto request, IMapper mapper, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync(new object[] { id }, ct);
        if (user is null) return (false, "Пользователь не найден");

        mapper.Map(request, user);

        // Sync permissions
        var current = await db.UserPermissions.Where(up => up.UserId == id).ToListAsync(ct);
        db.UserPermissions.RemoveRange(current);

        foreach (var permId in request.Permissions)
        {
            if (await db.Permissions.AnyAsync(p => p.Id == permId && !p.IsDeleted, ct))
                db.UserPermissions.Add(new TenantUserPermission { UserId = id, PermissionId = permId });
        }

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public static async Task<(bool Success, string? Error)> DeleteUserAsync(this TenantDbContext db, Guid id, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync(new object[] { id }, ct);
        if (user is null) return (false, "Пользователь не найден");

        user.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public static async Task<(bool Success, string? Error)> ChangePasswordAsync(this TenantDbContext db, ChangeUserPasswordDto dto, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync(new object[] { dto.UserId }, ct);
        if (user is null) return (false, "Пользователь не найден");

        user.HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }
}
