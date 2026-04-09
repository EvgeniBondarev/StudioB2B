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
                <!DOCTYPE html>
                <html lang="ru">
                <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background:#f4f4f7;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
                    <tr>
                      <td align="center" style="padding:48px 16px;">
                        <table width="480" cellpadding="0" cellspacing="0" role="presentation" style="max-width:480px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                          <tr>
                            <td style="background:#4f46e5;padding:32px 40px;text-align:center;">
                              <span style="color:#ffffff;font-size:24px;font-weight:700;letter-spacing:1px;">StudioB2B</span>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:40px 40px 32px;">
                              <h1 style="margin:0 0 12px;font-size:20px;font-weight:700;color:#111827;">Подтверждение аккаунта</h1>
                              <p style="margin:0 0 8px;font-size:15px;color:#4b5563;line-height:1.6;">
                                Вас добавили в рабочее пространство <strong style="color:#111827;">StudioB2B</strong>.
                              </p>
                              <p style="margin:0 0 32px;font-size:15px;color:#4b5563;line-height:1.6;">
                                Нажмите кнопку ниже, чтобы подтвердить email и активировать аккаунт:
                              </p>
                              <div style="text-align:center;margin:0 0 32px;">
                                <a href="{activationUrl}"
                                   style="display:inline-block;padding:14px 36px;background:#4f46e5;color:#ffffff;border-radius:8px;text-decoration:none;font-weight:600;font-size:16px;letter-spacing:0.3px;">
                                  Активировать аккаунт
                                </a>
                              </div>
                              <p style="margin:0 0 12px;font-size:13px;color:#9ca3af;line-height:1.6;">
                                ⏱&nbsp; Ссылка действительна <strong>3 дня</strong>.
                              </p>
                              <p style="margin:0;font-size:13px;color:#9ca3af;line-height:1.6;">
                                Если кнопка не работает, скопируйте и откройте эту ссылку в браузере:<br>
                                <a href="{activationUrl}" style="color:#4f46e5;word-break:break-all;">{activationUrl}</a>
                              </p>
                            </td>
                          </tr>
                          <tr>
                            <td style="background:#f8f8fc;border-top:1px solid #e8e8f0;padding:20px 40px;text-align:center;">
                              <p style="margin:0;font-size:12px;color:#9ca3af;">StudioB2B &nbsp;·&nbsp; Это автоматическое сообщение, не отвечайте на него.</p>
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;

            await _emailService.SendAsync(email, email, "Подтверждение аккаунта — StudioB2B", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send activation email to {Email}", email);
        }
    }
}
