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
/// отправляет через одно постоянное SMTP-соединение — без лишних reconnect'ов.
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

        while (!stoppingToken.IsCancellationRequested)
        {
            using var smtp = new SmtpClient();
            try
            {
                var socketOpts = _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
                await smtp.ConnectAsync(_options.Host, _options.Port, socketOpts, stoppingToken);

                if (!string.IsNullOrWhiteSpace(_options.User))
                    await smtp.AuthenticateAsync(_options.User, _options.Password, stoppingToken);

                _logger.LogInformation("[EMAIL SERVICE] Подключён к {Host}:{Port}", _options.Host, _options.Port);

                await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await smtp.SendAsync(BuildMessage(item), stoppingToken);
                        _logger.LogDebug("[EMAIL SERVICE] Отправлено → {To}: {Subject}", item.ToAddress, item.Subject);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "[EMAIL SERVICE] Ошибка отправки → {To}", item.ToAddress);
                        if (!smtp.IsConnected)
                            break; // разрыв соединения — выходим на переподключение
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMAIL SERVICE] Ошибка SMTP-соединения, повтор через 30 с");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            finally
            {
                if (smtp.IsConnected)
                    await smtp.DisconnectAsync(true, CancellationToken.None);
            }
        }
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

    private record EmailItem(string ToAddress, string ToName, string Subject, string HtmlBody);
}

