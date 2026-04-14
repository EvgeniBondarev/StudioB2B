using System.Threading.Channels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Фоновый сервис отправки почты. Принимает письма мгновенно (Channel),
/// открывает SMTP-соединение только на время отправки пакета — без простаивающих соединений.
/// </summary>
public class BackgroundEmailSenderService : BackgroundService, IEmailService
{
    private readonly Channel<EmailItem> _channel = Channel.CreateUnbounded<EmailItem>(
        new UnboundedChannelOptions { SingleReader = true });

    private readonly EmailOptions _options;
    private readonly ILogger<BackgroundEmailSenderService> _logger;

    public BackgroundEmailSenderService(IOptions<EmailOptions> options, ILogger<BackgroundEmailSenderService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Ставит письмо в очередь. Возвращается немедленно.</summary>
    public Task SendAsync(string toAddress, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        _channel.Writer.TryWrite(new EmailItem(toAddress, toName, subject, htmlBody));
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("[EMAIL SERVICE] SMTP не настроен — письма будут отброшены.");
            await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
                _logger.LogWarning("[EMAIL NOT SENT — SMTP not configured] To: {To} | Subject: {Subject}", item.ToAddress, item.Subject);
            return;
        }

        await foreach (var first in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            var batch = new List<EmailItem> { first };
            while (_channel.Reader.TryRead(out var next))
                batch.Add(next);

            await SendBatchAsync(batch, stoppingToken);
        }
    }

    private async Task SendBatchAsync(List<EmailItem> batch, CancellationToken ct)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var smtp = new SmtpClient();
            try
            {
                var socketOpts = _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
                await smtp.ConnectAsync(_options.Host, _options.Port, socketOpts, ct);

                if (!string.IsNullOrWhiteSpace(_options.User))
                    await smtp.AuthenticateAsync(_options.User, _options.Password, ct);

                _logger.LogInformation("[EMAIL SERVICE] Подключён к {Host}:{Port}, отправка {Count} писем", _options.Host, _options.Port, batch.Count);

                foreach (var item in batch)
                {
                    await smtp.SendAsync(BuildMessage(item), ct);
                    _logger.LogDebug("[EMAIL SERVICE] Отправлено → {To}: {Subject}", item.ToAddress, item.Subject);
                }

                await smtp.DisconnectAsync(true, CancellationToken.None);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMAIL SERVICE] Попытка {Attempt}/{Max} — ошибка SMTP, повтор через {Delay} с", attempt, maxAttempts, 10 * attempt);
                if (smtp.IsConnected)
                    await smtp.DisconnectAsync(false, CancellationToken.None);

                if (attempt < maxAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(10 * attempt), ct);
            }
        }

        _logger.LogError("[EMAIL SERVICE] Не удалось отправить {Count} писем после {Max} попыток", batch.Count, maxAttempts);
    }

    private MimeMessage BuildMessage(EmailItem item)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        msg.To.Add(new MailboxAddress(item.ToName, item.ToAddress));
        msg.Subject = item.Subject;
        msg.Body = new TextPart("html") { Text = item.HtmlBody };
        return msg;
    }

    private sealed record EmailItem(string ToAddress, string ToName, string Subject, string HtmlBody);
}

