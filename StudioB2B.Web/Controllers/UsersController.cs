using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// API контроллер для работы с пользователями тенанта
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITenantProvider _tenantProvider;

    public UsersController(IUserService userService, ITenantProvider tenantProvider)
    {
        _userService = userService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Получить список всех пользователей тенанта
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Получить пользователя по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Создать пользователя
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var (success, error) = await _userService.CreateUserAsync(request);
        if (!success)
            return BadRequest(new { error });

        return Ok();
    }

    /// <summary>
    /// Обновить пользователя
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto request)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var (success, error) = await _userService.UpdateUserAsync(id, request);
        if (!success)
            return BadRequest(new { error });

        return Ok();
    }

    /// <summary>
    /// Удалить пользователя
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var (success, error) = await _userService.DeleteUserAsync(id);
        if (!success)
            return BadRequest(new { error });

        return NoContent();
    }
}

