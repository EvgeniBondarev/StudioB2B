using Microsoft.AspNetCore.Mvc;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Web.Controllers;

[Route("api/auth")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantService _tenantService;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        IAuthService authService,
        ITenantProvider tenantProvider,
        ITenantService tenantService,
        IWebHostEnvironment env)
    {
        _authService = authService;
        _tenantProvider = tenantProvider;
        _tenantService = tenantService;
        _env = env;
    }

    // POST /api/auth/login  — принимает как form, так и JSON
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromForm] string? email,
        [FromForm] string? password,
        [FromForm] string? returnUrl,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return RedirectToLogin(returnUrl, "Введите email и пароль");

        string? token = _tenantProvider.IsResolved
            ? await _authService.LoginTenantAsync(email, password, ct)
            : await _authService.LoginMasterAsync(email, password, ct);

        if (token is null)
            return RedirectToLogin(returnUrl, "Неверный email или пароль");

        AppendAuthCookie(token);

        return Redirect(ToLocalPath(returnUrl));
    }

    [HttpPost("register-tenant")]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request, CancellationToken ct)
    {
        if (_tenantProvider.IsResolved)
            return BadRequest(new { message = "Недоступно внутри тенанта." });

        var result = await _tenantService.RegisterAsync(
            request.CompanyName,
            request.Subdomain,
            request.AdminEmail,
            request.AdminPassword,
            request.AdminSurname,
            request.AdminFirstName,
            request.AdminPatronymic,
            ct);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { tenantId = result.TenantId });
    }

    [HttpGet("logout")]
    [HttpPost("logout")]
    public IActionResult Logout([FromQuery] string? returnUrl)
    {
        Response.Cookies.Delete("auth_token");
        var redirect = string.IsNullOrWhiteSpace(returnUrl)
            ? (_tenantProvider.IsResolved ? "/" : "/login")
            : ToLocalPath(returnUrl);
        return Redirect(redirect);
    }

    public IActionResult RedirectToLogin(string? returnUrl, string error)
    {
        var localPath = ToLocalPath(returnUrl);
        var encoded = Uri.EscapeDataString(localPath);
        var errorEncoded = Uri.EscapeDataString(error);
        var loginPath = _tenantProvider.IsResolved ? "/" : "/login";
        return Redirect($"{loginPath}?returnUrl={encoded}&error={errorEncoded}");
    }

    /// <summary>
    /// Extracts the local path+query from a URL.
    /// Handles both absolute (https://host/path?q) and relative (/path?q) URLs.
    /// Always returns a relative path, defaulting to "/" if empty or invalid.
    /// </summary>
    private static string ToLocalPath(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "/";

        if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
            return string.IsNullOrEmpty(abs.PathAndQuery) ? "/" : abs.PathAndQuery;

        // Already relative — ensure it starts with /
        return url.StartsWith('/') ? url : "/" + url;
    }

    private void AppendAuthCookie(string token)
    {
        Response.Cookies.Append("auth_token", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_env.IsDevelopment(),
            Expires = DateTimeOffset.UtcNow.AddDays(14)
        });
    }
}

