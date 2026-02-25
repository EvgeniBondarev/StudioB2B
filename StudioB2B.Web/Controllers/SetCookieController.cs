using Microsoft.AspNetCore.Mvc;
using StudioB2B.Web.Services;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// Обменивает одноразовый временный ключ на JWT и устанавливает HttpOnly-куку.
/// Вызывается через полный редирект браузера (forceLoad: true) из Blazor-компонента,
/// поэтому HTTP-ответ ещё не отправлен и заголовки доступны для записи.
/// </summary>
[Route("auth")]
public class SetCookieController : Controller
{
    private readonly TokenExchangeService _exchange;
    private readonly IWebHostEnvironment _env;

    public SetCookieController(TokenExchangeService exchange, IWebHostEnvironment env)
    {
        _exchange = exchange;
        _env = env;
    }

    // GET /auth/set-cookie?t={key}&r={returnUrl}
    [HttpGet("set-cookie")]
    public IActionResult SetCookie([FromQuery] string t, [FromQuery] string? r)
    {
        var jwt = _exchange.Consume(t);

        if (jwt is null)
            return Redirect("/login");

        Response.Cookies.Append("auth_token", jwt, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_env.IsDevelopment(),
            Expires = DateTimeOffset.UtcNow.AddDays(14)
        });

        var returnUrl = string.IsNullOrWhiteSpace(r) ? "/" : Uri.UnescapeDataString(r);
        return Redirect(returnUrl);
    }

    // GET /auth/logout
    [HttpGet("logout")]
    public IActionResult Logout([FromQuery] string? r)
    {
        Response.Cookies.Delete("auth_token");
        var returnUrl = string.IsNullOrWhiteSpace(r) ? "/" : Uri.UnescapeDataString(r);
        return Redirect(returnUrl);
    }
}

