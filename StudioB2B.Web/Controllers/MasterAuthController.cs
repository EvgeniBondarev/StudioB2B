using Microsoft.AspNetCore.Mvc;
using StudioB2B.Infrastructure.Services;
using StudioB2B.Shared;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// Авторизация пользователей master-уровня.
/// </summary>
[Route("api/master/auth")]
[ApiController]
public class MasterAuthController : ControllerBase
{
    private readonly MasterAuthService _authService;
    private readonly ILogger<MasterAuthController> _logger;

    public MasterAuthController(MasterAuthService authService, ILogger<MasterAuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Авторизация master-пользователя. Возвращает JWT-токен.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] MasterLoginDto request, CancellationToken ct = default)
    {
        var result = await _authService.LoginAsync(request, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Master login failed for {Email}", request.Email);
            return Unauthorized(new { error = result.Error });
        }

        _logger.LogInformation("Master user logged in: {Email}", request.Email);
        return Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
    }

    /// <summary>
    /// Регистрация нового master-пользователя. Возвращает JWT-токен (авто-вход).
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] MasterRegisterDto request, CancellationToken ct = default)
    {
        var result = await _authService.RegisterAsync(request, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Master registration failed for {Email}: {Error}", request.Email, result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Master user registered: {Email}", request.Email);
        return Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
    }
}
