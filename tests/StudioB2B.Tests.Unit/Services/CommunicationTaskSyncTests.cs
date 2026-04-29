using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services.Communication;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

public class CommunicationTaskSyncTests
{
    private static DbContextOptions<TenantDbContext> InMemoryOptions(string dbName) =>
        new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

    private static TenantDbContext CreateInMemoryContext(string dbName) =>
        new(InMemoryOptions(dbName), currentUserProvider: null);

    private sealed class InMemoryFactory(string dbName) : ITenantDbContextFactory
    {
        public TenantDbContext CreateDbContext() => new(InMemoryOptions(dbName), currentUserProvider: null);
    }

    private static CommunicationTaskSyncService CreateService(
        string dbName,
        IOzonChatService? chatService = null,
        IOzonQuestionsService? questionsService = null,
        IOzonReviewsService? reviewsService = null)
    {
        return new CommunicationTaskSyncService(
            new InMemoryFactory(dbName),
            chatService ?? EmptyChatService(),
            questionsService ?? EmptyQuestionsService(),
            reviewsService ?? EmptyReviewsService(),
            NullLogger<CommunicationTaskSyncService>.Instance);
    }

    private static IOzonChatService EmptyChatService()
    {
        var mock = new Mock<IOzonChatService>();
        mock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonChatPageDto());
        mock.Setup(s => s.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ulong?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OzonChatHistoryResponseDto?)null);
        return mock.Object;
    }

    private static IOzonQuestionsService EmptyQuestionsService()
    {
        var mock = new Mock<IOzonQuestionsService>();
        mock.Setup(s => s.GetQuestionsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonQuestionPageDto());
        return mock.Object;
    }

    private static IOzonReviewsService EmptyReviewsService()
    {
        var mock = new Mock<IOzonReviewsService>();
        mock.Setup(s => s.GetReviewsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonReviewPageDto());
        return mock.Object;
    }

    [Fact]
    public async Task SyncAsync_AllServicesReturnEmpty_Returns0()
    {
        var dbName = nameof(SyncAsync_AllServicesReturnEmpty_Returns0);
        await using var ctx = CreateInMemoryContext(dbName);
        var svc = CreateService(dbName);

        var result = await svc.SyncAsync();

        result.Should().Be(0);
    }

    [Fact]
    public async Task SyncAsync_ChatServiceReturnsChatsButNoMatchingTasksInDb_Returns0()
    {
        var dbName = nameof(SyncAsync_ChatServiceReturnsChatsButNoMatchingTasksInDb_Returns0);
        await using var ctx = CreateInMemoryContext(dbName);
        ctx.SuppressAudit = true;

        // DB has no CommunicationTasks that match the chat external IDs
        var chatMock = new Mock<IOzonChatService>();
        chatMock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonChatPageDto
            {
                Chats = [new OzonChatViewModelDto { ChatId = "chat-123", ChatStatus = "OPENED", UnreadCount = 2 }]
            });
        chatMock.Setup(s => s.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ulong?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OzonChatHistoryResponseDto?)null);

        var svc = CreateService(dbName, chatService: chatMock.Object);

        var result = await svc.SyncAsync();

        result.Should().Be(0, "no existing tasks to update");
    }

    [Fact]
    public async Task SyncAsync_DoneTaskWithUnreadMessages_ReopensTask()
    {
        var dbName = nameof(SyncAsync_DoneTaskWithUnreadMessages_ReopensTask);
        await using var ctx = CreateInMemoryContext(dbName);
        ctx.SuppressAudit = true;

        const string chatId = "chat-reopened";
        var task = new CommunicationTask
        {
            Id = Guid.NewGuid(),
            TaskType = CommunicationTaskType.Chat,
            ExternalId = chatId,
            Status = CommunicationTaskStatus.Done,
            MarketplaceClientId = Guid.NewGuid()
        };
        ctx.CommunicationTasks.Add(task);
        await ctx.SaveChangesAsync();

        var chatMock = new Mock<IOzonChatService>();
        chatMock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonChatPageDto
            {
                Chats = [new OzonChatViewModelDto { ChatId = chatId, ChatStatus = "OPENED", UnreadCount = 3, LastMessageUserType = "Customer" }]
            });
        // GetChatHistoryAsync for AutoReopenDoneChatsAsync — no unread from buyer (return null)
        chatMock.Setup(s => s.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ulong?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OzonChatHistoryResponseDto?)null);

        var svc = CreateService(dbName, chatService: chatMock.Object);

        var result = await svc.SyncAsync();

        result.Should().BeGreaterThan(0, "task with unread messages must be reopened");

        var updatedTask = await ctx.CommunicationTasks.AsNoTracking()
            .FirstAsync(t => t.ExternalId == chatId);
        updatedTask.Status.Should().Be(CommunicationTaskStatus.New, "Done task with unread must be reopened");
    }

    [Fact]
    public async Task SyncAsync_ChatServiceThrows_Returns0AndNoException()
    {
        var dbName = nameof(SyncAsync_ChatServiceThrows_Returns0AndNoException);
        await using var ctx = CreateInMemoryContext(dbName);

        var chatMock = new Mock<IOzonChatService>();
        chatMock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        var svc = CreateService(dbName, chatService: chatMock.Object);

        var act = () => svc.SyncAsync();

        await act.Should().NotThrowAsync("exceptions in sync must be caught internally");
        var result = await svc.SyncAsync();
        result.Should().Be(0);
    }

    [Fact]
    public async Task SyncRecentAsync_AllServicesReturnEmpty_Returns0()
    {
        var dbName = nameof(SyncRecentAsync_AllServicesReturnEmpty_Returns0);
        await using var ctx = CreateInMemoryContext(dbName);
        var svc = CreateService(dbName);

        var result = await svc.SyncRecentAsync();

        result.Should().Be(0);
    }
}
