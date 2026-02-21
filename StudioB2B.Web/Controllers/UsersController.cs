using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Services;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// API контроллер для работы с пользователями тенанта
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Требуется авторизация
public class UsersController : ControllerBase
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<UsersController> _logger;

    // контекст создаётся один раз за экземпляр контроллера
    private TenantDbContext? _db;
    private TenantDbContext Db => _db ??= _dbContextFactory.CreateDbContext();

    public UsersController(
        ITenantDbContextFactory dbContextFactory,
        ITenantProvider tenantProvider,
        ILogger<UsersController> logger)
    {
        _dbContextFactory = dbContextFactory;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Получить список всех пользователей тенанта
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        if (!_tenantProvider.IsResolved)
        {
            return BadRequest(new { error = "Tenant not resolved. Use subdomain like demo.localhost:5184" });
        }

        _logger.LogInformation(
            "Getting users for tenant {TenantId} ({Subdomain})",
            _tenantProvider.TenantId,
            _tenantProvider.Subdomain);

        using var db = _dbContextFactory.CreateDbContext();
        
        var users = await db.Users
            .AsNoTracking()
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email!,
                FullName = u.FullName,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc,
                LastLoginAtUtc = u.LastLoginAtUtc
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Получить пользователя по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        if (!_tenantProvider.IsResolved)
        {
            return BadRequest(new { error = "Tenant not resolved" });
        }

        using var db = _dbContextFactory.CreateDbContext();
        
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email!,
                FullName = u.FullName,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc,
                LastLoginAtUtc = u.LastLoginAtUtc
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Деактивировать пользователя
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        if (!_tenantProvider.IsResolved)
        {
            return BadRequest(new { error = "Tenant not resolved" });
        }

        using var db = _dbContextFactory.CreateDbContext();
        var user = await db.Users.FindAsync(id);
        
        if (user == null)
            return NotFound();

        user.IsActive = false;
        await db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deactivated in tenant {TenantId}", id, _tenantProvider.TenantId);

        return Ok();
    }

    /// <summary>
    /// Активировать пользователя
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        if (!_tenantProvider.IsResolved)
        {
            return BadRequest(new { error = "Tenant not resolved" });
        }

        using var db = _dbContextFactory.CreateDbContext();
        var user = await db.Users.FindAsync(id);
        
        if (user == null)
            return NotFound();

        user.IsActive = true;
        await db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} activated in tenant {TenantId}", id, _tenantProvider.TenantId);

        return Ok();
    }
}

/// <summary>
/// DTO для передачи данных пользователя
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}
