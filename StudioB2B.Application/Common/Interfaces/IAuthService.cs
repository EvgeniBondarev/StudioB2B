namespace StudioB2B.Application.Common.Interfaces;

public interface IAuthService
{
    Task<string?> LoginMasterAsync(string email, string password, CancellationToken ct = default);

    Task<string?> LoginTenantAsync(string email, string password, CancellationToken ct = default);

    Task<string?> RegisterMasterAsync(string email, string password, CancellationToken ct = default);
}

