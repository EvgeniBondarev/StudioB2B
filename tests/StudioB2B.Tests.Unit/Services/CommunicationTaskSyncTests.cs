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

    private sealed class StubTenantProvider : ITenantProvider
    {
        public Guid? TenantId { get; } = Guid.NewGuid();
        public string? Subdomain => null;
        public string? ConnectionString => "stub";
        public bool IsResolved => true;
        public bool RequireLoginCode => false;
        public bool RequireEmailActivation => false;
    }

    private static async Task SeedClientAsync(string dbName, Guid clientId, string name = "TestShop")
    {
        await using var ctx = CreateInMemoryContext(dbName);
        ctx.SuppressAudit = true;
        ctx.MarketplaceClients!.Add(new MarketplaceClient
        {
            Id = clientId,
            Name = name,
            ApiId = "api-test",
            Key = "key-test",
            IsDeleted = false
        });
        await ctx.SaveChangesAsync();
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
            NullLogger<CommunicationTaskSyncService>.Instance,
            Mock.Of<ITaskBoardNotificationSender>(),
            new StubTenantProvider());
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
        var svc = CreateService(dbName);

        var result = await svc.SyncAsync();

        result.Should().Be(0);
    }

    [Fact]
    public async Task SyncAsync_NewChat_InsertsTask()
    {
        var dbName = nameof(SyncAsync_NewChat_InsertsTask);
        var clientId = Guid.NewGuid();
        await SeedClientAsync(dbName, clientId);

        var chatMock = new Mock<IOzonChatService>();
        chatMock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonChatPageDto
            {
                Chats =
                [
                    new OzonChatViewModelDto
                    {
                        ChatId = "chat-new-1",
                        ChatStatus = "OPENED",
                        MarketplaceClientId = clientId,
                        MarketplaceClientName = "TestShop",
                        CreatedAt = DateTime.UtcNow
                    }
                ]
            });
        chatMock.Setup(s => s.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ulong?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OzonChatHistoryResponseDto?)null);

        var svc = CreateService(dbName, chatService: chatMock.Object);
        var result = await svc.SyncAsync();

        result.Should().BeGreaterThan(0, "new chat must be inserted");

        await using var ctx = CreateInMemoryContext(dbName);
        var task = await ctx.CommunicationTasks.FirstOrDefaultAsync(t => t.ExternalId == "chat-new-1");
        task.Should().NotBeNull();
        task!.TaskType.Should().Be(CommunicationTaskType.Chat);
        task.Status.Should().Be(CommunicationTaskStatus.New);
        task.Title.Should().Be("Чат TestShop");
    }

    [Fact]
    public async Task SyncAsync_TerminalChatStatus_NotInserted()
    {
        var dbName = nameof(SyncAsync_TerminalChatStatus_NotInserted);
        var clientId = Guid.NewGuid();
        await SeedClientAsync(dbName, clientId);

        var chatMock = new Mock<IOzonChatService>();
        chatMock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonChatPageDto
            {
                Chats =
                [
                    new OzonChatViewModelDto
                    {
                        ChatId = "chat-closed-1",
                        ChatStatus = "CLOSED",
                        MarketplaceClientId = clientId,
                        MarketplaceClientName = "TestShop",
                        CreatedAt = DateTime.UtcNow
                    }
                ]
            });
        chatMock.Setup(s => s.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ulong?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OzonChatHistoryResponseDto?)null);

        var svc = CreateService(dbName, chatService: chatMock.Object);
        await svc.SyncAsync();

        await using var ctx = CreateInMemoryContext(dbName);
        var exists = await ctx.CommunicationTasks.AnyAsync(t => t.ExternalId == "chat-closed-1");
        exists.Should().BeFalse("terminal-status chat must not be inserted");
    }

    [Fact]
    public async Task SyncAsync_NewQuestion_InsertsTask()
    {
        var dbName = nameof(SyncAsync_NewQuestion_InsertsTask);
        var clientId = Guid.NewGuid();
        await SeedClientAsync(dbName, clientId);

        var qMock = new Mock<IOzonQuestionsService>();
        qMock.Setup(s => s.GetQuestionsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonQuestionPageDto
            {
                Questions =
                [
                    new OzonQuestionViewModelDto
                    {
                        Id = "q-new-1",
                        Status = "NEW",
                        Text = "Какой размер?",
                        MarketplaceClientId = clientId,
                        MarketplaceClientName = "TestShop",
                        PublishedAt = DateTime.UtcNow
                    }
                ]
            });

        var svc = CreateService(dbName, questionsService: qMock.Object);
        var result = await svc.SyncAsync();

        result.Should().BeGreaterThan(0);

        await using var ctx = CreateInMemoryContext(dbName);
        var task = await ctx.CommunicationTasks.FirstOrDefaultAsync(t => t.ExternalId == "q-new-1");
        task.Should().NotBeNull();
        task!.TaskType.Should().Be(CommunicationTaskType.Question);
        task.Title.Length.Should().BeLessThanOrEqualTo(500);
    }

    [Fact]
    public async Task SyncAsync_NewReview_InsertsTask()
    {
        var dbName = nameof(SyncAsync_NewReview_InsertsTask);
        var clientId = Guid.NewGuid();
        await SeedClientAsync(dbName, clientId);

        var rMock = new Mock<IOzonReviewsService>();
        rMock.Setup(s => s.GetReviewsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonReviewPageDto
            {
                Reviews =
                [
                    new OzonReviewViewModelDto
                    {
                        Id = "r-new-1",
                        Status = "NEW",
                        Rating = 5,
                        Text = "Отличный товар!",
                        MarketplaceClientId = clientId,
                        MarketplaceClientName = "TestShop",
                        PublishedAt = DateTime.UtcNow
                    }
                ]
            });

        var svc = CreateService(dbName, reviewsService: rMock.Object);
        var result = await svc.SyncAsync();

        result.Should().BeGreaterThan(0);

        await using var ctx = CreateInMemoryContext(dbName);
        var task = await ctx.CommunicationTasks.FirstOrDefaultAsync(t => t.ExternalId == "r-new-1");
        task.Should().NotBeNull();
        task!.TaskType.Should().Be(CommunicationTaskType.Review);
        task.Title.Length.Should().BeLessThanOrEqualTo(500);
    }

    [Fact]
    public async Task SyncAsync_ExistingChatUpdatesPreviewText()
    {
        var dbName = nameof(SyncAsync_ExistingChatUpdatesPreviewText);
        var clientId = Guid.NewGuid();
        await SeedClientAsync(dbName, clientId);

        await using var ctx = CreateInMemoryContext(dbName);
        ctx.SuppressAudit = true;
        ctx.CommunicationTasks.Add(new CommunicationTask
        {
            Id = Guid.NewGuid(),
            TaskType = CommunicationTaskType.Chat,
            ExternalId = "chat-preview-1",
            Status = CommunicationTaskStatus.New,
            MarketplaceClientId = clientId,
            Title = "Чат TestShop",
            ExternalStatus = "OPENED"
        });
        await ctx.SaveChangesAsync();

        const string expectedPreview = "Привет, как дела?";
        var chatMock = new Mock<IOzonChatService>();
        chatMock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonChatPageDto
            {
                Chats =
                [
                    new OzonChatViewModelDto
                    {
                        ChatId = "chat-preview-1",
                        ChatStatus = "OPENED",
                        MarketplaceClientId = clientId,
                        MarketplaceClientName = "TestShop"
                    }
                ]
            });
        chatMock.Setup(s => s.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ulong?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonChatHistoryResponseDto
            {
                Messages = [new OzonChatMessageDto { Data = [expectedPreview] }]
            });

        var svc = CreateService(dbName, chatService: chatMock.Object);
        await svc.SyncAsync();

        var updated = await ctx.CommunicationTasks.AsNoTracking()
            .FirstAsync(t => t.ExternalId == "chat-preview-1");
        updated.PreviewText.Should().Be(expectedPreview);
    }

    [Fact]
    public async Task SyncAsync_DoneTaskWithUnreadMessages_ReopensTask()
    {
        var dbName = nameof(SyncAsync_DoneTaskWithUnreadMessages_ReopensTask);
        const string chatId = "chat-reopened";
        var clientId = Guid.NewGuid();
        await SeedClientAsync(dbName, clientId);

        await using var ctx = CreateInMemoryContext(dbName);
        ctx.SuppressAudit = true;
        ctx.CommunicationTasks.Add(new CommunicationTask
        {
            Id = Guid.NewGuid(),
            TaskType = CommunicationTaskType.Chat,
            ExternalId = chatId,
            Status = CommunicationTaskStatus.Done,
            MarketplaceClientId = clientId,
            Title = "Чат TestShop",
            ExternalStatus = "OPENED"
        });
        await ctx.SaveChangesAsync();

        var chatMock = new Mock<IOzonChatService>();
        chatMock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonChatPageDto
            {
                Chats =
                [
                    new OzonChatViewModelDto
                    {
                        ChatId = chatId,
                        ChatStatus = "OPENED",
                        UnreadCount = 3,
                        LastMessageUserType = "Customer",
                        MarketplaceClientId = clientId,
                        MarketplaceClientName = "TestShop"
                    }
                ]
            });
        chatMock.Setup(s => s.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ulong?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OzonChatHistoryResponseDto?)null);

        var svc = CreateService(dbName, chatService: chatMock.Object);
        var result = await svc.SyncAsync();

        result.Should().BeGreaterThan(0);

        var updatedTask = await ctx.CommunicationTasks.AsNoTracking()
            .FirstAsync(t => t.ExternalId == chatId);
        updatedTask.Status.Should().Be(CommunicationTaskStatus.New);
    }

    [Fact]
    public async Task SyncAsync_ChatServiceThrows_Returns0AndNoException()
    {
        var dbName = nameof(SyncAsync_ChatServiceThrows_Returns0AndNoException);
        await SeedClientAsync(dbName, Guid.NewGuid());

        var chatMock = new Mock<IOzonChatService>();
        chatMock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        var svc = CreateService(dbName, chatService: chatMock.Object);

        var act = () => svc.SyncAsync();

        await act.Should().NotThrowAsync();
        var result = await svc.SyncAsync();
        result.Should().Be(0);
    }

    [Fact]
    public async Task UpsertChatAsync_NewChat_InsertsTask()
    {
        var dbName = nameof(UpsertChatAsync_NewChat_InsertsTask);
        var clientId = Guid.NewGuid();
        await SeedClientAsync(dbName, clientId);
        const string chatId = "chat-upsert-new";

        var chatMock = new Mock<IOzonChatService>();
        chatMock.Setup(s => s.GetChatsPageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OzonChatPageDto
            {
                Chats =
                [
                    new OzonChatViewModelDto
                    {
                        ChatId = chatId,
                        ChatStatus = "OPENED",
                        MarketplaceClientId = clientId,
                        MarketplaceClientName = "TestShop",
                        CreatedAt = DateTime.UtcNow
                    }
                ]
            });
        chatMock.Setup(s => s.GetChatHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ulong?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OzonChatHistoryResponseDto?)null);

        var svc = CreateService(dbName, chatService: chatMock.Object);
        await svc.UpsertChatAsync(chatId, OzonPushMessageType.NewMessage);

        await using var ctx = CreateInMemoryContext(dbName);
        var task = await ctx.CommunicationTasks.FirstOrDefaultAsync(t => t.ExternalId == chatId);
        task.Should().NotBeNull();
        task!.TaskType.Should().Be(CommunicationTaskType.Chat);
        task.Status.Should().Be(CommunicationTaskStatus.New);
    }

    [Fact]
    public async Task SyncRecentAsync_AllServicesReturnEmpty_Returns0()
    {
        var dbName = nameof(SyncRecentAsync_AllServicesReturnEmpty_Returns0);
        var svc = CreateService(dbName);

        var result = await svc.SyncRecentAsync();

        result.Should().Be(0);
    }
}
