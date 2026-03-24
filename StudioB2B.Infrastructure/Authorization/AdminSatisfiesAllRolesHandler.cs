using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace StudioB2B.Infrastructure.Authorization;

/// <summary>
/// Если у пользователя есть claim <c>full_access = true</c> (выдаётся при наличии Permission
/// с IsFullAccess = true), он автоматически удовлетворяет любому role-требованию.
/// Иначе — проверяются обычные role-claims из JWT.
/// </summary>
public class AdminSatisfiesAllRolesHandler : AuthorizationHandler<RolesAuthorizationRequirement>
{
    private const string FullAccessClaim = "full_access";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RolesAuthorizationRequirement requirement)
    {
        // Full-access permission → bypass all role checks
        if (context.User.HasClaim(FullAccessClaim, "true"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Otherwise check individual role claims (page / function / column names)
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
