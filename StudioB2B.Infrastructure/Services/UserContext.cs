using System.Security.Claims;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Scoped-сервис, хранящий ClaimsPrincipal текущего пользователя.
/// Заполняется из Blazor-контекста (MainLayout), поскольку в Blazor Server
/// IHttpContextAccessor.HttpContext недоступен после WebSocket-рукопожатия.
/// </summary>
public class UserContext
{
    private ClaimsPrincipal? _principal;

    /// <summary>
    /// Текущий principal. Null — пользователь не аутентифицирован.
    /// </summary>
    public ClaimsPrincipal? Principal => _principal;

    /// <summary>
    /// Устанавливает текущего пользователя (вызывается из MainLayout после первого рендера).
    /// </summary>
    public void SetUser(ClaimsPrincipal? principal) => _principal = principal;
}

