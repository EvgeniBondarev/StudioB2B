using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Master;
using StudioB2B.Domain.Entities.Tenant;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly MasterDbContext _masterDb;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtOptions _jwt;

    // TenantDbContext is optional — null when no tenant is resolved
    private readonly TenantDbContext? _tenantDb;

    public AuthService(
        MasterDbContext masterDb,
        IPasswordHasher passwordHasher,
        IOptions<JwtOptions> jwt,
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantProvider tenantProvider)
    {
        _masterDb = masterDb;
        _passwordHasher = passwordHasher;
        _jwt = jwt.Value;

        if (tenantProvider.IsResolved)
            _tenantDb = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<string?> LoginMasterAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _masterDb.Users
                       .GetByPredicateAsync(u => u.Email == email, ct);

        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
            return null;

        return GenerateMasterToken(user);
    }

    public async Task<string?> LoginTenantAsync(string email, string password, CancellationToken ct = default)
    {
        if (_tenantDb is null)
            return null;

        var user = await _tenantDb.Users.GetByPredicateAsync(u => u.Email == email, ct);

        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
            return null;

        return GenerateTenantToken(user);
    }

    public async Task<string?> RegisterMasterAsync(string email, string password, CancellationToken ct = default)
    {
        var exists = await _masterDb.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
            return null;

        var adminRole = await _masterDb.Roles.GetByPredicateAsync(r => r.Name == "Администратор", ct);

        var user = new MasterUser
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            Roles = adminRole is not null ? [adminRole] : []
        };

        _masterDb.Users.Add(user);
        await _masterDb.SaveChangesAsync(ct);

        return GenerateMasterToken(user);
    }

    private string GenerateMasterToken(MasterUser masterUser)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, masterUser.Id.ToString()),
            new(ClaimTypes.Email, masterUser.Email),
            new(ClaimTypes.Name, masterUser.Email)
        };

        foreach (var role in masterUser.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role.Name));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwt.ExpirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateTenantToken(TenantUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var fullName = $"{user.Surname} {user.FirstName} {user.Patronymic}".Trim();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName),
            new("surname", user.Surname),
            new("firstName", user.FirstName),
            new("patronymic", user.Patronymic)
        };

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role.Name));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_jwt.ExpirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

