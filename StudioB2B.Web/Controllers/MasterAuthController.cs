using Microsoft.AspNetCore.Mvc;
using StudioB2B.Infrastructure.Features.Master;

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
        [FromBody] MasterLoginRequest request, CancellationToken ct = default)
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
}
