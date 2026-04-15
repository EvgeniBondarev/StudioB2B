using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Services;
using Xunit;

namespace StudioB2B.Tests.Unit.Email;

public class SmtpEmailServiceTests
{
    private static (SmtpEmailService Service, Mock<ILogger<SmtpEmailService>> Logger) Create(EmailOptions opts)
    {
        var options = new OptionsWrapper<EmailOptions>(opts);
        var logger = new Mock<ILogger<SmtpEmailService>>();
        return (new SmtpEmailService(options, logger.Object), logger);
    }

    [Fact]
    public async Task SendAsync_EmptyHost_DoesNotThrow()
    {
        var (svc, _) = Create(new EmailOptions { Host = "" });

        var act = () => svc.SendAsync("test@example.com", "Test User", "Subject", "<p>Body</p>");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_EmptyHost_LogsWarning()
    {
        var (svc, logger) = Create(new EmailOptions { Host = "" });

        await svc.SendAsync("test@example.com", "Test User", "Test Subject", "<p>Body</p>");

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WhitespaceHost_LogsWarning()
    {
        var (svc, logger) = Create(new EmailOptions { Host = "   " });

        await svc.SendAsync("to@example.com", "Name", "Subj", "<b>hi</b>");

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

