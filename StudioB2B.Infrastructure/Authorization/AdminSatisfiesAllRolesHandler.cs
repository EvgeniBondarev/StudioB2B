using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace StudioB2B.Infrastructure.Authorization;

/// <summary>
/// Если пользователь имеет роль Admin, он удовлетворяет любому требованию ролей.
/// Решает проблему, когда у администратора тенанта есть только Admin, а страницы требуют PriceTypesView и т.д.
/// </summary>
public class AdminSatisfiesAllRolesHandler : AuthorizationHandler<RolesAuthorizationRequirement>
{
    private const string AdminRole = "Admin";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RolesAuthorizationRequirement requirement)
    {
        if (context.User.IsInRole(AdminRole))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        foreach (var role in requirement.AllowedRoles)
        {
            if (context.User.IsInRole(role))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}
