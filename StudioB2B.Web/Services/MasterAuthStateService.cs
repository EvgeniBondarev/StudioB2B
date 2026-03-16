namespace StudioB2B.Web.Services;

/// <summary>
/// Scoped-сервис для синхронизации состояния master-авторизации
/// между компонентами одной Blazor-цепи (Circuit).
/// </summary>
public class MasterAuthStateService
{
    private bool _isAuthenticated;
    private MasterUserInfo? _userInfo;

    public bool IsAuthenticated => _isAuthenticated;
    public MasterUserInfo? UserInfo => _userInfo;

    public event Action? OnChanged;

    public void Set(bool isAuthenticated, MasterUserInfo? userInfo)
    {
        _isAuthenticated = isAuthenticated;
        _userInfo = userInfo;
        OnChanged?.Invoke();
    }

    public void Clear()
    {
        _isAuthenticated = false;
        _userInfo = null;
        OnChanged?.Invoke();
    }
}

