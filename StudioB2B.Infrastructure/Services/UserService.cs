using AutoMapper;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис для работы с пользователями тенанта.
/// Инкапсулирует работу с БД, используя extension-методы из UserFeatures.
/// </summary>
public class UserService : IUserService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ITenantDbContextFactory dbContextFactory,
        IMapper mapper,
        IEmailService emailService,
        ILogger<UserService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<List<UserListDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        return await db.GetUsersAsync(_mapper, ct);
    }

    public async Task<UserListDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        return await db.GetUserByIdAsync(id, _mapper, ct);
    }

    public async Task<(bool Success, string? Error)> CreateUserAsync(CreateUserDto request, string tenantBaseUrl, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        var (ok, error, tokenId) = await db.CreateUserAsync(request, _mapper, ct);

        if (!ok || tokenId is null)
            return (false, error);

        var activationUrl = $"{tenantBaseUrl.TrimEnd('/')}/activate?token={tokenId}";
        _ = SendActivationEmailAsync(request.Email, activationUrl);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ActivateUserAsync(Guid tokenId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        return await db.ActivateUserAsync(tokenId, ct);
    }

    public async Task<(bool Success, string? Error)> UpdateUserAsync(Guid id, UpdateUserDto request, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        return await db.UpdateUserAsync(id, request, _mapper, ct);
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        return await db.DeleteUserAsync(id, ct);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(ChangeUserPasswordDto dto, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        return await db.ChangePasswordAsync(dto, ct);
    }

    private async Task SendActivationEmailAsync(string email, string activationUrl)
    {
        _logger.LogInformation("Activation link for {Email}: {Url}", email, activationUrl);

        try
        {
            var html = $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
                  <h2>Подтверждение аккаунта</h2>
                  <p>Вас добавили в рабочее пространство <strong>StudioB2B</strong>.</p>
                  <p>Нажмите кнопку ниже, чтобы подтвердить email и активировать аккаунт:</p>
                  <a href="{activationUrl}"
                     style="display:inline-block;padding:0.75rem 1.5rem;background:#6366f1;color:#fff;
                            border-radius:8px;text-decoration:none;font-weight:600;margin:1rem 0;">
                    Активировать аккаунт
                  </a>
                  <p style="color:#888;font-size:0.875rem">Ссылка действительна 3 дня.</p>
                </div>
                """;

            await _emailService.SendAsync(email, email, "Подтверждение аккаунта — StudioB2B", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send activation email to {Email}", email);
        }
    }
}
