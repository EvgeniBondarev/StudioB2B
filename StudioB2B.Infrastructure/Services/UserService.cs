using AutoMapper;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис для работы с пользователями тенанта.
/// Инкапсулирует работу с БД, используя extension-методы из UserFeatures.
/// </summary>
public class UserService : IUserService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IMapper _mapper;

    public UserService(ITenantDbContextFactory dbContextFactory, IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _mapper = mapper;
    }

    public async Task<List<UserListDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetUsersAsync(_mapper, ct);
    }

    public async Task<UserListDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetUserByIdAsync(id, _mapper, ct);
    }

    public async Task<(bool Success, string? Error)> CreateUserAsync(CreateUserDto request, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.CreateUserAsync(request, _mapper, ct);
    }

    public async Task<(bool Success, string? Error)> UpdateUserAsync(Guid id, UpdateUserDto request, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.UpdateUserAsync(id, request, _mapper, ct);
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.DeleteUserAsync(id, ct);
    }
}

