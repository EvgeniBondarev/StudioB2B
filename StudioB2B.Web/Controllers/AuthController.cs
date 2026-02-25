using Microsoft.AspNetCore.Mvc;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Web.Models;

namespace StudioB2B.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
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

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        string? token;

        if (_tenantProvider.IsResolved)
            token = await _authService.LoginTenantAsync(request.Email, request.Password, ct);
        else
            token = await _authService.LoginMasterAsync(request.Email, request.Password, ct);

        if (token is null)
            return Unauthorized(new { message = "Неверный email или пароль." });

        AppendAuthCookie(token);
        return Ok(new { message = "OK" });
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (_tenantProvider.IsResolved)
            return BadRequest(new { message = "Регистрация недоступна в тенанте." });

        var token = await _authService.RegisterMasterAsync(request.Email, request.Password, ct);

        if (token is null)
            return Conflict(new { message = "Пользователь с таким email уже существует." });

        AppendAuthCookie(token);
        return Ok(new { message = "OK" });
    }

    // POST /api/auth/register-tenant
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
            ct);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { tenantId = result.TenantId });
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token");
        return Ok(new { message = "OK" });
    }

    private void AppendAuthCookie(string token)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_env.IsDevelopment(),
            Expires = DateTimeOffset.UtcNow.AddDays(14)
        };
        Response.Cookies.Append("auth_token", token, options);
    }
}

