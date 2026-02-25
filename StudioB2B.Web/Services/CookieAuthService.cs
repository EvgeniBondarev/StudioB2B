using StudioB2B.Application.Common.Interfaces;

namespace StudioB2B.Web.Services;

/// <summary>
/// Выполняет аутентификацию и возвращает URL для редиректа.
/// JWT устанавливается как HttpOnly-кука через GET /auth/set-cookie?t={key},
/// потому что в Blazor Server HTTP-ответ уже отправлен к моменту
/// выполнения обработчиков событий — напрямую записать куку невозможно.
/// </summary>
public class CookieAuthService
{
    private readonly IAuthService _authService;
    private readonly ITenantProvider _tenantProvider;
    private readonly TokenExchangeService _exchange;

    public CookieAuthService(
        IAuthService authService,
        ITenantProvider tenantProvider,
        TokenExchangeService exchange)
    {
        _authService = authService;
        _tenantProvider = tenantProvider;
        _exchange = exchange;
    }

    /// <summary>
    /// Логин. Возвращает redirect URL с временным ключом, либо null при неверных данных.
    /// </summary>
    public async Task<string?> LoginAsync(string email, string password,
        string returnUrl = "/", CancellationToken ct = default)
    {
        var token = _tenantProvider.IsResolved
            ? await _authService.LoginTenantAsync(email, password, ct)
            : await _authService.LoginMasterAsync(email, password, ct);

        if (token is null) return null;

        return BuildRedirectUrl(token, returnUrl);
    }

    /// <summary>
    /// Регистрация мастер-пользователя. Возвращает redirect URL, либо null если email занят.
    /// </summary>
    public async Task<string?> RegisterMasterAsync(string email, string password,
        string returnUrl = "/", CancellationToken ct = default)
    {
        var token = await _authService.RegisterMasterAsync(email, password, ct);
        if (token is null) return null;

        return BuildRedirectUrl(token, returnUrl);
    }

    private string BuildRedirectUrl(string jwt, string returnUrl)
    {
        var key = _exchange.Store(jwt);
        var encodedReturn = Uri.EscapeDataString(returnUrl);
        return $"/auth/set-cookie?t={key}&r={encodedReturn}";
    }
}

