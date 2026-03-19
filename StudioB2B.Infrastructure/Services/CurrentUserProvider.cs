using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Провайдер информации о текущем пользователе.
/// Приоритет: UserContext (Blazor Server) → HttpContext (API-контроллеры).
/// </summary>
public class CurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContext _userContext;

    public CurrentUserProvider(IHttpContextAccessor httpContextAccessor, UserContext userContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
    }

    /// <summary>
    /// Возвращает principal из UserContext (Blazor) или из HttpContext (API).
    /// </summary>
    private ClaimsPrincipal? User =>
        (_userContext.Principal?.Identity?.IsAuthenticated == true
            ? _userContext.Principal
            : null)
        ?? _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            // Сначала ищем по длинному ClaimTypes (после JWT-маппинга),
            // затем по короткому JWT-имени "sub" (если маппинг не был применён).
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? Email =>
        User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Permissions =>
        User?.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
        ?? Enumerable.Empty<string>();
}
