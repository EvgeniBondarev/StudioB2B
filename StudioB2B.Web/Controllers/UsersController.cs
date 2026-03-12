using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// API контроллер для работы с пользователями тенанта
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ITenantProvider _tenantProvider;

    public UsersController(ITenantDbContextFactory dbContextFactory, ITenantProvider tenantProvider)
    {
        _dbContextFactory = dbContextFactory;
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

        using var db = _dbContextFactory.CreateDbContext();

        var users = await db.Users
            .AsNoTracking()
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive
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
            return BadRequest(new { error = "Tenant not resolved" });

        using var db = _dbContextFactory.CreateDbContext();

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive
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
            return BadRequest(new { error = "Tenant not resolved" });

        using var db = _dbContextFactory.CreateDbContext();
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = false;
        await db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Активировать пользователя
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        using var db = _dbContextFactory.CreateDbContext();
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = true;
        await db.SaveChangesAsync();
        return Ok();
    }
}

