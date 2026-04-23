using Microsoft.AspNetCore.Components;
using Radzen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using StudioB2B.Domain.Constants;
using StudioB2B.Shared;
using StudioB2B.Web.Components.Common.TaskBoard;

namespace StudioB2B.Web.Components.Common;

public partial class CommunicationTaskBoard
{
    private TaskBoardDto _board = new();
    private Guid? _myTenantId;
    private HashSet<CommunicationTaskType> _filterTypes = new();
    private DateTime? _filterFrom = DateTime.Today.AddDays(-30);
    private DateTime? _filterTo;
    private HashSet<Guid> _filterClientIds = new();
    private List<ClientOptionDto> _clientOptions = new();
    private HashSet<Guid> _filterUserIds = new();
    private List<UserListDto> _userOptions = new();
    private bool _sidebarOpen;
    private bool _loading;
    private bool _loadingNew;
    private bool _syncing;
    private bool _loadingMore;

    private const int DonePageSize = 25;
    private ElementReference _doneSentinel;
    private DotNetObjectReference<CommunicationTaskBoard>? _dotNetRef;

    private HubConnection? _hubConnection;
    private Timer? _timerTick;
    private Timer? _chatPollTimer;
    private bool _isPollingChat;
    private DateTime _lastRefreshAt;

    private static readonly string[] ChatQuickReplies =
    [
        "Здравствуйте!",
        "Здравствуйте, для проверки необходим VIN номер Вашего авто",
        "Заявка на рассмотрении, ожидайте",
        "Оформите возврат"
    ];

    private static readonly string[] QuestionQuickReplies =
    [
        "Здравствуйте!",
        "Здравствуйте, для проверки необходим VIN номер Вашего авто",
        "Заявка на рассмотрении, ожидайте",
        "Оформите возврат"
    ];

    private static readonly string[] ReviewQuickReplies =
    [
        "Здравствуйте!",
        "Здравствуйте! Спасибо за ваш отзыв.",
        "Здравствуйте! Приносим извинения за доставленные неудобства.",
        "Спасибо за обратную связь, мы обязательно учтём ваше мнение.",
        "Пожалуйста, напишите нам в личные сообщения, мы поможем решить вопрос.",
        "Мы рады, что вам понравился товар!"
    ];

    // Preview state
    private CommunicationTaskDto? _previewTask;
    private bool _previewLoading;
    private CommunicationTaskDetailDto? _taskDetail;
    private List<OzonChatMessageDto> _chatMessages = new();
    private OzonChatViewModelDto? _previewChatVm;
    private string? _chatFallbackUrl;
    private OzonQuestionDetailDto? _questionDetail;
    private OzonReviewDetailDto? _reviewDetail;

    private Dictionary<string, string> _chatMessageAuthors = new();
    private Dictionary<string, string> _questionAuthors = new();
    private Dictionary<string, string> _reviewAuthors = new();

    // Overlay compose state
    private bool _overlaySendingAnswer;
    private bool _overlayBusy;
    private string? _overlayDeletingAnswerId;
    private string? _overlayDeletingCommentId;

    // Gallery ref
    private TaskBoardGallery _gallery = null!;

    private List<CommunicationTaskDto> FilteredNew
    {
        get
        {
            var src = _filterTypes.Count == 0
                ? _board.NewTasks.AsEnumerable()
                : _board.NewTasks.Where(t => _filterTypes.Contains(t.TaskType));
            return src
                .OrderByDescending(t => t.WasPreviouslyCompleted)
                .ThenByDescending(t => t.CreatedAt)
                .ToList();
        }
    }

    private List<CommunicationTaskDto> FilteredInProgress
    {
        get
        {
            var list = ApplyTypeFilter(_board.InProgressTasks);
            if (_previewTask is not null)
            {
                var idx = list.FindIndex(t => t.Id == _previewTask.Id);
                if (idx > 0)
                {
                    var item = list[idx];
                    list.RemoveAt(idx);
                    list.Insert(0, item);
                }
            }
            return list;
        }
    }

    private List<CommunicationTaskDto> FilteredDone =>
        ApplyTypeFilter(_board.DoneTasks)
            .OrderByDescending(t => t.CompletedAt ?? t.UpdatedAt)
            .ToList();

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!TenantProvider.IsResolved) return;

        var authState = await AuthState;
        UserContext.SetUser(authState.User);
        _myTenantId = await TaskService.GetCurrentUserTenantIdAsync();

        _ = LoadClientOptionsAsync();
        _ = LoadUserOptionsAsync();

        if (BoardState.HasData)
        {
            _board = BoardState.Board!;
            await StartSignalRAsync();
            _timerTick = new Timer(_ => InvokeAsync(StateHasChanged), null,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            if (BoardState.IsStale)
                _ = SilentReloadAsync();
        }
        else
        {
            await QuickSyncAndReloadAsync();
            await StartSignalRAsync();
            _timerTick = new Timer(_ => InvokeAsync(StateHasChanged), null,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        BoardState.PendingChatOpenChanged += OnPendingChatOpenChanged;

        try { await TryOpenPendingChatAsync(); }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Ошибка открытия чата", ex.Message, 5000);
        }

        if (_previewTask is null && BoardState.ActivePreviewExternalId is not null)
        {
            var savedTask = FindTaskInBoard(BoardState.ActivePreviewExternalId, BoardState.ActivePreviewClientId);
            if (savedTask is not null)
                await ShowPreviewAsync(savedTask);
            else
                BoardState.ClearActivePreview();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            try { await Js.InvokeVoidAsync("taskBoardEsc.init", _dotNetRef); }
            catch { /* ignore */ }
        }

        if (_board.DoneTasks.Count < _board.DoneTotalCount && !_loadingMore)
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            try { await Js.InvokeVoidAsync("taskBoardInfiniteScroll.init", _doneSentinel, _dotNetRef); }
            catch { /* JS not ready on first render */ }
        }
    }

    [JSInvokable]
    public void OnEscapeKeyAsync()
    {
        if (_previewTask is not null)
            ClosePreview();
    }

    private async Task QuickSyncAndReloadAsync()
    {
        await LoadBoardAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                var reopened = await SyncService.SyncRecentAsync();
                await InvokeAsync(async () =>
                {
                    if (reopened > 0)
                        NotificationService.Notify(NotificationSeverity.Warning, "Задачи возвращены в работу",
                            $"{reopened} {TaskBoardHelpers.Plural(reopened, "задача", "задачи", "задач")} возвращено — покупатель написал снова", 6000);
                    await SilentReloadAsync();
                });
            }
            catch { /* non-critical */ }
        });
    }

    private async Task FullSyncAndReloadAsync()
    {
        await LoadBoardAsync();
        _syncing = true;
        StateHasChanged();
        try
        {
            var reopened = await SyncService.SyncAsync();
            var msg = reopened > 0
                ? $"Полная синхронизация завершена. {reopened} {TaskBoardHelpers.Plural(reopened, "задача", "задачи", "задач")} возвращено в работу."
                : "Полная синхронизация завершена";
            NotificationService.Notify(NotificationSeverity.Success, "Синхронизация", msg, 4000);
        }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Warning, "Синхронизация", ex.Message, 4000); }
        finally { _syncing = false; }

        await SilentReloadAsync();
    }

    private async Task LoadBoardAsync()
    {
        _loading = true;
        _loadingNew = false;
        StateHasChanged();
        try
        {
            var filter = BuildFilter();

            _board = await TaskService.GetDbBoardAsync(filter);
            _loading = false;
            _loadingNew = true;
            StateHasChanged();

            await foreach (var batch in TaskService.StreamNewTasksAsync(filter))
            {
                MergeNewTaskBatch(_board, batch);
                await InvokeAsync(StateHasChanged);
            }
            BoardState.Set(_board);
        }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000); }
        finally { _loading = false; _loadingNew = false; StateHasChanged(); }
    }

    private async Task SilentReloadAsync()
    {
        try
        {
            var filter = BuildFilter();
            var dbBoard = await TaskService.GetDbBoardAsync(filter);
            _board = dbBoard;
            await InvokeAsync(StateHasChanged);

            await foreach (var batch in TaskService.StreamNewTasksAsync(filter))
            {
                MergeNewTaskBatch(_board, batch);
                await InvokeAsync(StateHasChanged);
            }
            BoardState.Set(_board);
            await InvokeAsync(StateHasChanged);
        }
        catch { /* silent */ }
    }

    [JSInvokable]
    public async Task LoadMoreDoneAsync()
    {
        if (_loadingMore || _board.DoneTasks.Count >= _board.DoneTotalCount) return;

        _loadingMore = true;
        StateHasChanged();
        try
        {
            var filter = BuildFilter();
            var (items, total) = await TaskService.GetDoneTasksPageAsync(filter, _board.DoneTasks.Count, DonePageSize);
            _board.DoneTasks.AddRange(items);
            _board.DoneTotalCount = total;
            BoardState.AppendDone(items, total);
        }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000); }
        finally { _loadingMore = false; StateHasChanged(); }
    }

    private async Task RefreshBoardSilentAsync()
    {
        try
        {
            var board = await TaskService.GetBoardAsync(BuildFilter());
            _board = board;
            BoardState.Set(board);
            _lastRefreshAt = DateTime.UtcNow;
            StateHasChanged();
        }
        catch { /* silent */ }
    }

    private CommunicationTaskFilter BuildFilter() => new()
    {
        DoneTake = DonePageSize,
        From = _filterFrom,
        To = _filterTo,
        TaskTypes = _filterTypes.ToList(),
        MarketplaceClientIds = _filterClientIds.ToList(),
        AssignedToUserIds = _filterUserIds.ToList()
    };

    private async Task StartSignalRAsync()
    {
        try
        {
            var baseUri = Navigation.BaseUri.TrimEnd('/');
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUri}/hubs/taskboard")
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<object>("TaskClaimed", async _ => await InvokeAsync(OnSignalRBoardUpdate));
            _hubConnection.On<object>("TaskReleased", async _ => await InvokeAsync(OnSignalRBoardUpdate));
            _hubConnection.On<object>("TaskCompleted", async _ => await InvokeAsync(OnSignalRBoardUpdate));
            _hubConnection.On<object>("BoardUpdated", async _ => await InvokeAsync(OnSignalRBoardUpdate));

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("JoinTenantGroup", TenantProvider.TenantId.ToString());
        }
        catch { NotificationService.Notify(NotificationSeverity.Warning, "SignalR", "Не удалось подключиться"); }
    }

    private async Task OnSignalRBoardUpdate()
    {
        if ((DateTime.UtcNow - _lastRefreshAt).TotalSeconds < 2) return;
        await RefreshBoardSilentAsync();
    }

    private async Task ToggleTypeFilter((CommunicationTaskType type, bool add) args)
    {
        if (args.add) _filterTypes.Add(args.type); else _filterTypes.Remove(args.type);
        await LoadBoardAsync();
    }

    private async Task ToggleClientFilter((Guid id, bool add) args)
    {
        if (args.add) _filterClientIds.Add(args.id); else _filterClientIds.Remove(args.id);
        await LoadBoardAsync();
    }

    private async Task ToggleUserFilter((Guid id, bool add) args)
    {
        if (args.add) _filterUserIds.Add(args.id); else _filterUserIds.Remove(args.id);
        await ReloadDbAndKeepNewAsync();
    }

    private async Task ReloadDbAndKeepNewAsync()
    {
        try
        {
            var savedNew = _board.NewTasks.ToList();
            var dbBoard = await TaskService.GetDbBoardAsync(BuildFilter());
            MergeNewTaskBatch(dbBoard, savedNew);
            _board = dbBoard;
            BoardState.Set(_board);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000); }
    }

    private string GetCurrentUserDisplayName()
    {
        if (_myTenantId.HasValue)
        {
            var user = _userOptions.FirstOrDefault(u => u.Id == _myTenantId.Value);
            if (user is not null)
                return (user.FirstName + " " + user.LastName).Trim();
        }
        return UserContext.Principal?.Identity?.Name ?? "Сотрудник";
    }

    private static void MergeNewTaskBatch(TaskBoardDto board, IEnumerable<CommunicationTaskDto> batch)
    {
        var existing = board.NewTasks
            .Select(t => (t.TaskType, t.ExternalId, t.MarketplaceClientId))
            .ToHashSet();

        foreach (var t in batch)
        {
            if (existing.Add((t.TaskType, t.ExternalId, t.MarketplaceClientId)))
            {
                board.NewTasks.Add(t);
                board.TypeCounts[t.TaskType] = board.TypeCounts.GetValueOrDefault(t.TaskType) + 1;
            }
        }

        board.NewTasks.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
    }

    private async Task LoadClientOptionsAsync()
    {
        try { _clientOptions = await MarketplaceClientService.GetClientOptionsAsync(); StateHasChanged(); }
        catch { /* non-critical */ }
    }

    private async Task LoadUserOptionsAsync()
    {
        try { _userOptions = await UserService.GetAllUsersAsync(); StateHasChanged(); }
        catch { /* non-critical */ }
    }

    private List<CommunicationTaskDto> ApplyTypeFilter(List<CommunicationTaskDto> source)
    {
        var filtered = _filterTypes.Count == 0
            ? source.AsEnumerable()
            : source.Where(t => _filterTypes.Contains(t.TaskType));
        return filtered.OrderByDescending(t => t.CreatedAt).ToList();
    }

    private void OnPendingChatOpenChanged()
    {
        _ = InvokeAsync(async () =>
        {
            var pending = BoardState.TakePendingChatOpen();
            if (pending is null) return;
            try { await OpenChatAsync(pending.Value.ChatId, pending.Value.ClientId); }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Ошибка открытия чата", ex.Message, 5000);
            }
        });
    }

    public async Task OpenChatAsync(string chatId, Guid? clientId)
    {
        if (_clientOptions.Count == 0)
            await LoadClientOptionsAsync();

        var task = FindTaskInBoard(chatId, clientId.HasValue && clientId.Value != Guid.Empty ? clientId : null);

        if (task is null)
        {
            if (!clientId.HasValue || clientId.Value == Guid.Empty)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Чат не найден",
                    $"Чат не найден на доске задач. Возможно, кабинет не зарегистрирован.", 5000);
                return;
            }

            task = new CommunicationTaskDto
            {
                Id = Guid.Empty,
                ExternalId = chatId,
                MarketplaceClientId = clientId.Value,
                MarketplaceClientName = _clientOptions
                    .FirstOrDefault(c => c.Id == clientId.Value)?.Name ?? string.Empty,
                TaskType = CommunicationTaskType.Chat,
                Title = chatId
            };
        }

        await ShowPreviewAsync(task);
    }

    private async Task TryOpenPendingChatAsync()
    {
        var pending = BoardState.TakePendingChatOpen();
        if (pending is null) return;
        await OpenChatAsync(pending.Value.ChatId, pending.Value.ClientId);
    }

    private async Task OnCardClick(CommunicationTaskDto task) => await ShowPreviewAsync(task);

    private async Task ShowPreviewAsync(CommunicationTaskDto task)
    {
        _previewTask = task;
        _previewLoading = true;
        _taskDetail = null;
        _chatMessages.Clear();
        _previewChatVm = null;
        _chatFallbackUrl = null;
        _questionDetail = null;
        _reviewDetail = null;
        _chatMessageAuthors = new();
        _questionAuthors = new();
        _reviewAuthors = new();
        StateHasChanged();

        try
        {
            switch (task.TaskType)
            {
                case CommunicationTaskType.Chat:
                    _previewChatVm = new OzonChatViewModelDto
                    {
                        MarketplaceClientId = task.MarketplaceClientId,
                        MarketplaceClientName = task.MarketplaceClientName,
                        ChatId = task.ExternalId
                    };
                    try
                    {
                        var history = await ChatService.GetChatHistoryAsync(
                            task.MarketplaceClientId, task.ExternalId, limit: 50);
                        if (history is not null)
                            _chatMessages = history.Messages.OrderBy(m => m.CreatedAt).ToList();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _chatFallbackUrl = $"https://seller.ozon.ru/app/messenger/?group=customers_v2&id={task.ExternalId}";
                    }
                    _chatMessageAuthors = await TaskService.GetOutgoingAuthorsAsync(task.ExternalId, CommunicationTaskType.Chat);
                    break;

                case CommunicationTaskType.Question:
                {
                    var creds = await GetClientCredsAsync(task.MarketplaceClientId);
                    var qVm = new OzonQuestionViewModelDto
                    {
                        Id = task.ExternalId,
                        MarketplaceClientId = task.MarketplaceClientId,
                        MarketplaceClientName = task.MarketplaceClientName,
                        ApiId = creds.apiId,
                        ApiKey = creds.apiKey
                    };
                    _questionDetail = await QuestionsService.GetQuestionDetailAsync(qVm);
                    _questionAuthors = await TaskService.GetOutgoingAuthorsAsync(task.ExternalId, CommunicationTaskType.Question);
                    break;
                }

                case CommunicationTaskType.Review:
                {
                    var creds = await GetClientCredsAsync(task.MarketplaceClientId);
                    var rVm = new OzonReviewViewModelDto
                    {
                        Id = task.ExternalId,
                        MarketplaceClientId = task.MarketplaceClientId,
                        MarketplaceClientName = task.MarketplaceClientName,
                        ApiId = creds.apiId,
                        ApiKey = creds.apiKey
                    };
                    _reviewDetail = await ReviewsService.GetReviewDetailAsync(rVm);
                    _reviewAuthors = await TaskService.GetOutgoingAuthorsAsync(task.ExternalId, CommunicationTaskType.Review);
                    break;
                }
            }

            if (task.Id != Guid.Empty)
                _taskDetail = await TaskService.GetTaskDetailAsync(task.Id);
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Ошибка предпросмотра", ex.Message, 5000);
        }
        finally
        {
            _previewLoading = false;
            StateHasChanged();
            if (_previewTask?.TaskType == CommunicationTaskType.Chat)
            {
                await Js.InvokeVoidAsync("scrollChatOverlayToBottom");
                StartChatPolling();
            }
        }

        BoardState.SetActivePreview(task.ExternalId, task.MarketplaceClientId, task.TaskType);
    }

    private void ClosePreview()
    {
        StopChatPolling();
        _previewTask = null;
        _taskDetail = null;
        _chatMessages.Clear();
        _previewChatVm = null;
        _chatFallbackUrl = null;
        _questionDetail = null;
        _reviewDetail = null;
        _chatMessageAuthors = new();
        _questionAuthors = new();
        _reviewAuthors = new();
        _overlayBusy = false;
        _overlayDeletingAnswerId = null;
        _overlayDeletingCommentId = null;
        _gallery.Close();
        BoardState.ClearActivePreview();
        StateHasChanged();
    }

    private void StartChatPolling()
    {
        StopChatPolling();
        _chatPollTimer = new Timer(_ => InvokeAsync(PollChatHistoryAsync), null,
            TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
    }

    private void StopChatPolling()
    {
        _chatPollTimer?.Dispose();
        _chatPollTimer = null;
    }

    private async Task PollChatHistoryAsync()
    {
        if (_isPollingChat || _previewChatVm is null || _previewTask?.TaskType != CommunicationTaskType.Chat)
            return;
        _isPollingChat = true;
        try
        {
            ulong? fromId = _chatMessages.Count > 0 ? _chatMessages[^1].MessageId : null;
            var direction = fromId.HasValue ? "Forward" : "Backward";
            var result = await ChatService.GetChatHistoryAsync(
                _previewChatVm.MarketplaceClientId,
                _previewChatVm.ChatId,
                direction: direction,
                fromMessageId: fromId,
                limit: 50);
            if (result?.Messages is { Count: > 0 } newMessages)
            {
                var existingIds = _chatMessages.Select(m => m.MessageId).ToHashSet();
                var toAdd = newMessages
                    .Where(m => !existingIds.Contains(m.MessageId))
                    .OrderBy(m => m.CreatedAt)
                    .ToList();
                if (toAdd.Count > 0)
                {
                    _chatMessages.AddRange(toAdd);

                    var hasNewSellerMsg = toAdd.Any(m =>
                        m.User?.Type is "Seller" or "seller" or "Seller_Support" or "SELLER_SUPPORT" &&
                        !_chatMessageAuthors.ContainsKey(m.MessageId.ToString()));
                    if (hasNewSellerMsg && _previewTask is not null)
                    {
                        var freshAuthors = await TaskService.GetOutgoingAuthorsAsync(
                            _previewTask.ExternalId, CommunicationTaskType.Chat);
                        foreach (var kv in freshAuthors)
                            _chatMessageAuthors[kv.Key] = kv.Value;
                    }

                    StateHasChanged();
                    await Js.InvokeVoidAsync("scrollChatOverlayToBottom");
                }
            }
        }
        catch { /* silently ignore polling errors */ }
        finally { _isPollingChat = false; }
    }

    private void OpenOverlayGallery((List<string> items, int index, bool isVideo) args)
    {
        _gallery.Open(args.items, args.index, args.isVideo);
    }

    private async Task<Guid?> GetUserIdAsync()
    {
        if (CurrentUser.UserId is not null)
            return CurrentUser.UserId.Value;

        var authState = await AuthState;
        UserContext.SetUser(authState.User);

        if (CurrentUser.UserId is not null)
            return CurrentUser.UserId.Value;

        NotificationService.Notify(NotificationSeverity.Error, "Ошибка",
            "Не удалось определить текущего пользователя. Обновите страницу.", 5000);
        return null;
    }

    private async Task ClaimAsync(CommunicationTaskDto task)
    {
        var userId = await GetUserIdAsync();
        if (userId is null) return;

        _board.NewTasks.RemoveAll(t =>
            t.TaskType == task.TaskType &&
            t.ExternalId == task.ExternalId &&
            t.MarketplaceClientId == task.MarketplaceClientId);
        task.Status = CommunicationTaskStatus.InProgress;
        task.AssignedToUserId = _myTenantId ?? userId;
        task.HasActiveTimer = true;
        task.StartedAt = DateTime.UtcNow;
        _board.InProgressTasks.Insert(0, task);
        StateHasChanged();

        try
        {
            if (task.Id == Guid.Empty)
            {
                var newId = await TaskService.CreateAndClaimAsync(task, userId.Value);
                if (newId is null)
                    NotificationService.Notify(NotificationSeverity.Warning, "Задача уже занята", "Кто-то успел взять раньше");
                else
                    task.Id = newId.Value;
            }
            else
            {
                var ok = await TaskService.ClaimTaskAsync(task.Id, userId.Value);
                if (!ok)
                    NotificationService.Notify(NotificationSeverity.Warning, "Задача уже занята", "Кто-то успел взять раньше");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.InnerException?.Message ?? ex.Message, 8000);
        }

        await RefreshBoardSilentAsync();
    }

    private async Task PauseAsync(CommunicationTaskDto task)
    {
        var userId = await GetUserIdAsync();
        if (userId is null) return;

        var local = _board.InProgressTasks.FirstOrDefault(t => t.Id == task.Id);
        if (local is not null)
        {
            if (local.StartedAt.HasValue)
                local.TotalTimeSpentTicks += (DateTime.UtcNow - local.StartedAt.Value).Ticks;
            local.HasActiveTimer = false;
            local.StartedAt = null;
            StateHasChanged();
        }

        try { await TaskService.PauseTaskAsync(task.Id, userId.Value); }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000); }

        await RefreshBoardSilentAsync();
    }

    private async Task ResumeAsync(CommunicationTaskDto task)
    {
        var userId = await GetUserIdAsync();
        if (userId is null) return;

        var local = _board.InProgressTasks.FirstOrDefault(t => t.Id == task.Id);
        if (local is not null) { local.HasActiveTimer = true; local.StartedAt = DateTime.UtcNow; StateHasChanged(); }

        try { await TaskService.ResumeTaskAsync(task.Id, userId.Value); }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000); }

        await RefreshBoardSilentAsync();
    }

    private async Task CompleteAsync(CommunicationTaskDto task)
    {
        var userId = await GetUserIdAsync();
        if (userId is null) return;

        var liveTicks = task.TotalTimeSpentTicks;
        if (task.HasActiveTimer && task.StartedAt.HasValue)
            liveTicks += (DateTime.UtcNow - task.StartedAt.Value).Ticks;
        var totalMinutes = (decimal)TimeSpan.FromTicks(liveTicks).TotalMinutes;

        _board.InProgressTasks.RemoveAll(t => t.Id == task.Id);
        task.TotalTimeSpentTicks = liveTicks;
        task.Status = CommunicationTaskStatus.Done;
        task.HasActiveTimer = false;
        task.CompletedAt = DateTime.UtcNow;
        if (_board.ActiveRates.Count > 0)
        {
            var lines = CommunicationPaymentCalculator.ComputeBreakdownLines(
                totalMinutes, task.TaskType, task.AssignedToUserId, _board.ActiveRates);
            task.PaymentAmount = lines.Count > 0 ? Math.Round(lines.Sum(l => l.Amount), 2) : null;
        }
        _board.DoneTasks.Insert(0, task);
        _board.DoneTotalCount++;
        StateHasChanged();

        try
        {
            var ok = await TaskService.CompleteTaskAsync(task.Id, userId.Value);
            if (ok) NotificationService.Notify(NotificationSeverity.Success, "Задача выполнена", task.Title, 2000);
        }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000); }

        await RefreshBoardSilentAsync();
    }

    private async Task ReleaseAsync(CommunicationTaskDto task)
    {
        var userId = await GetUserIdAsync();
        if (userId is null) return;

        _board.InProgressTasks.RemoveAll(t => t.Id == task.Id);
        StateHasChanged();

        try { await TaskService.ReleaseTaskAsync(task.Id, userId.Value); }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000); }

        await RefreshBoardSilentAsync();
    }

    private async Task<(string apiId, string apiKey)> GetClientCredsAsync(Guid marketplaceClientId)
    {
        var creds = await MarketplaceClientService.GetClientCredentialsAsync(marketplaceClientId);
        return creds is not null ? (creds.Value.ApiId, creds.Value.Key) : ("", "");
    }

    private void RefreshPreviewTaskFromBoard()
    {
        if (_previewTask is null) return;
        var found = _board.NewTasks
            .Concat(_board.InProgressTasks)
            .FirstOrDefault(t =>
                t.ExternalId == _previewTask.ExternalId &&
                t.MarketplaceClientId == _previewTask.MarketplaceClientId);
        if (found is not null)
            _previewTask = found;
    }

    private CommunicationTaskDto? FindTaskInBoard(string externalId, Guid? clientId)
    {
        return _board.InProgressTasks
            .Concat(_board.NewTasks)
            .FirstOrDefault(t =>
                t.ExternalId == externalId &&
                (!clientId.HasValue || t.MarketplaceClientId == clientId.Value));
    }

    private async Task OverlayClaimAsync()
    {
        if (_previewTask is null) return;
        _overlayBusy = true;
        StateHasChanged();
        try
        {
            await ClaimAsync(_previewTask);
            RefreshPreviewTaskFromBoard();
            if (_previewTask?.Id != Guid.Empty && _previewTask?.Id is not null)
                _taskDetail = await TaskService.GetTaskDetailAsync(_previewTask.Id);
        }
        finally { _overlayBusy = false; StateHasChanged(); }
    }

    private async Task OverlayPauseAsync()
    {
        if (_previewTask is null) return;
        _overlayBusy = true;
        StateHasChanged();
        try { await PauseAsync(_previewTask); RefreshPreviewTaskFromBoard(); }
        finally { _overlayBusy = false; StateHasChanged(); }
    }

    private async Task OverlayResumeAsync()
    {
        if (_previewTask is null) return;
        _overlayBusy = true;
        StateHasChanged();
        try { await ResumeAsync(_previewTask); RefreshPreviewTaskFromBoard(); }
        finally { _overlayBusy = false; StateHasChanged(); }
    }

    private async Task OverlayCompleteAsync()
    {
        if (_previewTask is null) return;
        _overlayBusy = true;
        StateHasChanged();
        try { await CompleteAsync(_previewTask); ClosePreview(); }
        finally { _overlayBusy = false; }
    }

    private async Task OverlayReleaseAsync()
    {
        if (_previewTask is null) return;
        _overlayBusy = true;
        StateHasChanged();
        try { await ReleaseAsync(_previewTask); ClosePreview(); }
        finally { _overlayBusy = false; }
    }

    private async Task CardReopenAsync(CommunicationTaskDto task)
    {
        var userId = await GetUserIdAsync();
        if (userId is null) return;
        var ok = await TaskService.ReopenTaskAsync(task.Id, userId.Value);
        if (ok)
        {
            _board.DoneTasks.RemoveAll(t => t.Id == task.Id);
            task.Status = CommunicationTaskStatus.InProgress;
            task.AssignedToUserId = _myTenantId ?? userId;
            task.HasActiveTimer = true;
            task.StartedAt = DateTime.UtcNow;
            _board.InProgressTasks.Insert(0, task);
            StateHasChanged();
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Ошибка", "Не удалось вернуть задачу в работу");
        }
    }

    private async Task OverlayReopenAsync()
    {
        if (_previewTask is null) return;
        var userId = await GetUserIdAsync();
        if (userId is null) return;
        _overlayBusy = true;
        StateHasChanged();
        try
        {
            var ok = await TaskService.ReopenTaskAsync(_previewTask.Id, userId.Value);
            if (ok)
            {
                _board.DoneTasks.RemoveAll(t => t.Id == _previewTask.Id);
                _previewTask.Status = CommunicationTaskStatus.InProgress;
                _previewTask.AssignedToUserId = _myTenantId ?? userId;
                _previewTask.HasActiveTimer = true;
                _previewTask.StartedAt = DateTime.UtcNow;
                _board.InProgressTasks.Insert(0, _previewTask);
                ClosePreview();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Ошибка", "Не удалось вернуть задачу в работу");
            }
        }
        finally { _overlayBusy = false; StateHasChanged(); }
    }

    private async Task<bool> OverlaySendChatMessageAsync(string text, IBrowserFile? file, string? base64)
    {
        if (_previewChatVm is null) return false;
        try
        {
            if (file is not null && base64 is not null)
            {
                var (ok, err) = await ChatService.SendFileAsync(
                    _previewChatVm.MarketplaceClientId, _previewChatVm.ChatId, base64, file.Name);
                if (!ok)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Ошибка", err ?? "Не удалось отправить файл", 5000);
                    return false;
                }
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                var (ok, err) = await ChatService.SendMessageAsync(
                    _previewChatVm.MarketplaceClientId, _previewChatVm.ChatId, text);
                if (!ok)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Ошибка", err ?? "Не удалось отправить сообщение", 5000);
                    return false;
                }
            }

            var oldMessageIds = _chatMessages.Select(m => m.MessageId).ToHashSet();

            await Task.Delay(600);
            try
            {
                var history = await ChatService.GetChatHistoryAsync(
                    _previewChatVm.MarketplaceClientId, _previewChatVm.ChatId, limit: 50);
                if (history is not null)
                {
                    _chatMessages = history.Messages.OrderBy(m => m.CreatedAt).ToList();

                    if (_myTenantId.HasValue)
                    {
                        var myName = GetCurrentUserDisplayName();
                        var chatId = _previewChatVm.ChatId;
                        foreach (var msg in _chatMessages.Where(m =>
                            !oldMessageIds.Contains(m.MessageId) &&
                            (m.User?.Type is "Seller" or "seller" or "Seller_Support" or "SELLER_SUPPORT")))
                        {
                            var msgId = msg.MessageId.ToString();
                            if (!_chatMessageAuthors.ContainsKey(msgId))
                            {
                                _chatMessageAuthors[msgId] = myName;
                                _ = TaskService.RecordOutgoingMessageAsync(
                                    chatId, CommunicationTaskType.Chat, msgId, _myTenantId.Value, myName);
                            }
                        }
                    }
                }
                StateHasChanged();
                await Task.Delay(50);
                await Js.InvokeVoidAsync("scrollChatOverlayToBottom");
            }
            catch { /* ignore history reload failure */ }

            return true;
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000);
            return false;
        }
    }

    private async Task<bool> OverlaySendAnswerAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _previewTask is null || _questionDetail is null) return false;
        _overlaySendingAnswer = true;
        StateHasChanged();
        try
        {
            var qVm = _questionDetail.Question;
            var answerId = await QuestionsService.CreateQuestionAnswerAsync(qVm, text);
            if (answerId is not null)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Ответ отправлен", "", 2000);
                _questionDetail = await QuestionsService.GetQuestionDetailAsync(qVm);

                if (_myTenantId.HasValue)
                {
                    var myName = GetCurrentUserDisplayName();
                    _questionAuthors[answerId] = myName;
                    _ = TaskService.RecordOutgoingMessageAsync(
                        _previewTask.ExternalId, CommunicationTaskType.Question, answerId, _myTenantId.Value, myName);
                }

                StateHasChanged();
                return true;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Ошибка", "Не удалось отправить ответ", 5000);
                return false;
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000);
            return false;
        }
        finally { _overlaySendingAnswer = false; StateHasChanged(); }
    }

    private async Task<bool> OverlaySendCommentAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _previewTask is null) return false;
        if (string.IsNullOrWhiteSpace(_reviewDetail?.Review.Text))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Невозможно ответить", "На отзыв без текста нельзя оставить комментарий", 5000);
            return false;
        }
        try
        {
            var creds = await GetClientCredsAsync(_previewTask.MarketplaceClientId);
            var rVm = new OzonReviewViewModelDto
            {
                Id = _previewTask.ExternalId,
                MarketplaceClientId = _previewTask.MarketplaceClientId,
                MarketplaceClientName = _previewTask.MarketplaceClientName,
                ApiId = creds.apiId,
                ApiKey = creds.apiKey
            };
            var commentId = await ReviewsService.CreateReviewCommentAsync(rVm, text);
            if (commentId is not null)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Комментарий отправлен", "", 2000);
                _reviewDetail = await ReviewsService.GetReviewDetailAsync(rVm);

                if (_myTenantId.HasValue)
                {
                    var myName = GetCurrentUserDisplayName();
                    _reviewAuthors[commentId] = myName;
                    _ = TaskService.RecordOutgoingMessageAsync(
                        _previewTask.ExternalId, CommunicationTaskType.Review, commentId, _myTenantId.Value, myName);
                }

                StateHasChanged();
                return true;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Ошибка", "Не удалось отправить комментарий", 5000);
                return false;
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000);
            return false;
        }
    }

    private async Task OverlayDeleteAnswerAsync(OzonQuestionAnswerDto ans)
    {
        if (_previewTask is null || _questionDetail is null) return;
        _overlayDeletingAnswerId = ans.Id;
        StateHasChanged();
        try
        {
            var ok = await QuestionsService.DeleteQuestionAnswerAsync(_questionDetail.Question, ans.Id);
            if (ok)
            {
                _questionDetail?.Answers.Remove(ans);
                NotificationService.Notify(NotificationSeverity.Success, "Ответ удалён");
                StateHasChanged();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Ошибка", "Не удалось удалить ответ", 4000);
            }
        }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000); }
        finally { _overlayDeletingAnswerId = null; StateHasChanged(); }
    }

    private async Task OverlayDeleteCommentAsync(OzonReviewCommentDto comment)
    {
        if (_previewTask is null) return;
        _overlayDeletingCommentId = comment.Id;
        StateHasChanged();
        try
        {
            var creds = await GetClientCredsAsync(_previewTask.MarketplaceClientId);
            var rVm = new OzonReviewViewModelDto
            {
                Id = _previewTask.ExternalId,
                MarketplaceClientId = _previewTask.MarketplaceClientId,
                MarketplaceClientName = _previewTask.MarketplaceClientName,
                ApiId = creds.apiId,
                ApiKey = creds.apiKey
            };
            var ok = await ReviewsService.DeleteReviewCommentAsync(rVm, comment.Id);
            if (ok)
            {
                _reviewDetail?.Comments.Remove(comment);
                NotificationService.Notify(NotificationSeverity.Success, "Комментарий удалён");
                StateHasChanged();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Ошибка", "Не удалось удалить комментарий", 4000);
            }
        }
        catch (Exception ex) { NotificationService.Notify(NotificationSeverity.Error, "Ошибка", ex.Message, 5000); }
        finally { _overlayDeletingCommentId = null; StateHasChanged(); }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        BoardState.PendingChatOpenChanged -= OnPendingChatOpenChanged;
        _timerTick?.Dispose();
        StopChatPolling();
        _dotNetRef?.Dispose();
        try { await Js.InvokeVoidAsync("taskBoardInfiniteScroll.dispose"); }
        catch { /* ignore */ }
        try { await Js.InvokeVoidAsync("taskBoardEsc.dispose"); }
        catch { /* ignore */ }
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}

