using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис для работы с журналом изменений (аудитом) тенанта.
/// Инкапсулирует работу с БД, используя extension-методы из AuditLogFeatures.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public AuditLogService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<string>> GetFilterEntityNamesAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetAuditEntityNamesAsync(ct);
    }

    public async Task<List<string>> GetFilterUserNamesAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetAuditUserNamesAsync(ct);
    }

    public async Task<(List<FieldAuditLog> Items, int Total)> GetPagedAsync(
        AuditLogFilter filter,
        int skip,
        int take,
        string? orderBy,
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetAuditPagedAsync(filter, skip, take, orderBy, ct);
    }

    public async Task<List<FieldAuditLog>> GetByEntityAsync(
        string entityName,
        string entityId,
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetAuditByEntityAsync(entityName, entityId, ct);
    }

    public async Task<List<FieldAuditLog>> GetBySubjectsAsync(
        IReadOnlyList<AuditSubject> subjects,
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetAuditBySubjectsAsync(subjects, ct);
    }

    public async Task<IReadOnlyDictionary<string, string>> BuildValueResolverAsync(
        List<FieldAuditLog> logs,
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.BuildAuditValueResolverAsync(logs, ct);
    }
}

