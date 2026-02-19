using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services;
namespace StudioB2B.Infrastructure.Features.Users;
public record UserListDto(
    Guid Id, string Email, string FirstName, string LastName, string? MiddleName,
    bool IsActive, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc, List<string> Roles);
public record CreateUserRequest(
    string Email, string FirstName, string LastName, string? MiddleName,
    string Password, List<string> Roles);
public record UpdateUserRequest(
    string FirstName, string LastName, string? MiddleName, bool IsActive, List<string> Roles);
public record ChangePasswordRequest(string NewPassword);
public static class UserQueryExtensions
{
    public static IQueryable<ApplicationUser> OrderByLastName(this IQueryable<ApplicationUser> q)
        => q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
}
public class GetUsers(ITenantDbContextFactory factory)
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
            result.Add(new UserListDto(u.Id, u.Email, u.FirstName, u.LastName, u.MiddleName,
                u.IsActive, u.CreatedAtUtc, u.LastLoginAtUtc, roles));
        }
        return result;
    }
}
public class GetUserById(ITenantDbContextFactory factory)
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
        return new UserListDto(u.Id, u.Email, u.FirstName, u.LastName, u.MiddleName,
            u.IsActive, u.CreatedAtUtc, u.LastLoginAtUtc, roles);
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
public class CreateUser(ITenantDbContextFactory factory)
{
    public async Task<(bool Success, string? Error)> HandleAsync(
        CreateUserRequest request, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        using var um = UserManagerFactory.Create(db);
        var user = new ApplicationUser
        {
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
            EmailConfirmed = true,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            IsActive = true
        };
        var result = await um.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        foreach (var role in request.Roles)
            if (await db.Roles.AnyAsync(r => r.Name == role, ct))
                await um.AddToRoleAsync(user, role);
        return (true, null);
    }
}
public class UpdateUser(ITenantDbContextFactory factory)
{
    public async Task<(bool Success, string? Error)> HandleAsync(
        Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return (false, "Пользователь не найден");
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.MiddleName = request.MiddleName?.Trim();
        user.IsActive = request.IsActive;
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
public class ChangeUserPassword(ITenantDbContextFactory factory)
{
    public async Task<(bool Success, string? Error)> HandleAsync(
        Guid id, ChangePasswordRequest request, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return (false, "Пользователь не найден");
        using var um = UserManagerFactory.Create(db);
        var token = await um.GeneratePasswordResetTokenAsync(user);
        var result = await um.ResetPasswordAsync(user, token, request.NewPassword);
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
