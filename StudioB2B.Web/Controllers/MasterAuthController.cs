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
    /// Шаг 1 регистрации: создаёт пользователя и отправляет код на email.
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

        _logger.LogInformation("Verification code sent to {Email}", request.Email);
        return Ok(new { requiresVerification = result.RequiresVerification });
    }

    /// <summary>
    /// Шаг 2 регистрации: подтверждает код и возвращает JWT-токен.
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] MasterVerifyEmailDto request, CancellationToken ct = default)
    {
        var result = await _authService.VerifyEmailAsync(request, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Email verification failed for {Email}: {Error}", request.Email, result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Master user verified and logged in: {Email}", request.Email);
        return Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
    }

    /// <summary>
    /// Повторная отправка кода подтверждения.
    /// </summary>
    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode(
        [FromBody] ResendCodeRequest request, CancellationToken ct = default)
    {
        var result = await _authService.ResendCodeAsync(request.Email, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Resend code failed for {Email}: {Error}", request.Email, result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Verification code resent to {Email}", request.Email);
        return Ok(new { requiresVerification = true });
    }

    public record ResendCodeRequest(string Email);
}
