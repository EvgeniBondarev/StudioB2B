using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services.Communication;
using StudioB2B.Tests.Integration.Database;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

/// <summary>
/// Integration tests for CommunicationTaskSyncJob.UpsertChatTaskAsync.
/// Uses real MySQL via Testcontainers — no Ozon API calls for existing-task scenarios.
/// </summary>
[Collection("Database")]
public class CommunicationTaskSyncJobTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public CommunicationTaskSyncJobTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    private static CommunicationTaskSyncJob CreateJob()
    {
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var encryption = new Mock<IKeyEncryptionService>();
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns((string s) => s);

        var sender = new Mock<ITaskBoardNotificationSender>();

        return new CommunicationTaskSyncJob(
            encryption.Object,
            httpFactory.Object,
            NullLoggerFactory.Instance,
            sender.Object);
    }

    [Fact]
    public async Task UpsertChatTaskAsync_TypeChatClosed_ExistingTask_SetsClosedStatus()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var chatId = $"chat-close-{Guid.NewGuid():N}";
        var task = new CommunicationTask
        {
            Id = Guid.NewGuid(),
            TaskType = CommunicationTaskType.Chat,
            ExternalId = chatId,
            MarketplaceClientId = _fixture.DefaultClientId,
            Status = CommunicationTaskStatus.New,
            Title = "Test chat",
            ExternalStatus = "OPEN",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.CommunicationTasks.Add(task);
        await ctx.SaveChangesAsync();

        var job = CreateJob();
        await job.UpsertChatTaskAsync(
            Guid.NewGuid(),
            _fixture.ConnectionString,
            chatId,
            OzonPushMessageType.ChatClosed);

        var updated = await ctx.CommunicationTasks.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ExternalId == chatId);

        updated.Should().NotBeNull();
        updated!.ExternalStatus.Should().Be("CLOSED");
    }

    [Fact]
    public async Task UpsertChatTaskAsync_TypeNewMessage_ExistingDoneTask_ReOpensTask()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var chatId = $"chat-reopen-{Guid.NewGuid():N}";
        var task = new CommunicationTask
        {
            Id = Guid.NewGuid(),
            TaskType = CommunicationTaskType.Chat,
            ExternalId = chatId,
            MarketplaceClientId = _fixture.DefaultClientId,
            Status = CommunicationTaskStatus.Done,
            Title = "Test chat",
            ExternalStatus = "OPEN",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.CommunicationTasks.Add(task);
        await ctx.SaveChangesAsync();

        var job = CreateJob();
        await job.UpsertChatTaskAsync(
            Guid.NewGuid(),
            _fixture.ConnectionString,
            chatId,
            OzonPushMessageType.NewMessage);

        var updated = await ctx.CommunicationTasks.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ExternalId == chatId);

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(CommunicationTaskStatus.New);
        updated.WasPreviouslyCompleted.Should().BeTrue();
        updated.AssignedToUserId.Should().BeNull();
    }

    [Fact]
    public async Task UpsertChatTaskAsync_TypeNewMessage_ExistingActiveTask_DoesNotReopen()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var chatId = $"chat-active-{Guid.NewGuid():N}";
        var userId = Guid.NewGuid();
        var task = new CommunicationTask
        {
            Id = Guid.NewGuid(),
            TaskType = CommunicationTaskType.Chat,
            ExternalId = chatId,
            MarketplaceClientId = _fixture.DefaultClientId,
            Status = CommunicationTaskStatus.InProgress,
            AssignedToUserId = userId,
            Title = "Test chat",
            ExternalStatus = "OPEN",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.CommunicationTasks.Add(task);
        await ctx.SaveChangesAsync();

        var job = CreateJob();
        await job.UpsertChatTaskAsync(
            Guid.NewGuid(),
            _fixture.ConnectionString,
            chatId,
            OzonPushMessageType.NewMessage);

        var updated = await ctx.CommunicationTasks.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ExternalId == chatId);

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(CommunicationTaskStatus.InProgress);
        updated.AssignedToUserId.Should().Be(userId);
    }

    [Fact]
    public async Task UpsertChatTaskAsync_TypeChatClosed_NoExistingTask_DoesNothing()
    {
        var chatId = $"chat-noexist-{Guid.NewGuid():N}";

        var job = CreateJob();
        // Should not throw; no task exists so nothing to update
        await job.UpsertChatTaskAsync(
            Guid.NewGuid(),
            _fixture.ConnectionString,
            chatId,
            OzonPushMessageType.ChatClosed);

        await using var ctx = _fixture.CreateContext();
        var exists = await ctx.CommunicationTasks.AnyAsync(t => t.ExternalId == chatId);
        exists.Should().BeFalse();
    }
}

