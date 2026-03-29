using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Domain.Constants;

namespace StudioB2B.Infrastructure.Services.Modules;

public class ManufacturerModuleActivator : IModuleActivator
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ManufacturerModuleActivator> _logger;

    public string ModuleCode => ModuleCodes.Manufacturers;

    public ManufacturerModuleActivator(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<ManufacturerModuleActivator> logger)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task OnEnableAsync(TenantDbContext db, CancellationToken ct)
    {
        // 1. Clear existing manufacturer links from products
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE Products SET ManufacturerId = NULL WHERE ManufacturerId IS NOT NULL", ct);
        _logger.LogInformation("Cleared ManufacturerId on all products");

        // 2. Delete all manufacturers to reimport fresh
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Manufacturers", ct);
        _logger.LogInformation("Cleared Manufacturers table");

        // 3. Import from SQL dump
        var dumpPath = _configuration["Modules:Manufacturers:SqlDumpPath"];
        if (string.IsNullOrWhiteSpace(dumpPath))
        {
            _logger.LogWarning("Modules:Manufacturers:SqlDumpPath is not configured, skipping import");
            return;
        }

        if (!Path.IsPathRooted(dumpPath))
            dumpPath = Path.Combine(_hostEnvironment.ContentRootPath, dumpPath);

        if (!File.Exists(dumpPath))
        {
            _logger.LogError("SQL dump file not found at {Path}", dumpPath);
            return;
        }

        _logger.LogInformation("Importing manufacturers from {Path}...", dumpPath);

        var sql = await File.ReadAllTextAsync(dumpPath, ct);
        var statements = SplitSqlStatements(sql);

        var executed = 0;
        foreach (var statement in statements)
        {
            if (string.IsNullOrWhiteSpace(statement))
                continue;

            var lines = statement.Split('\n');
            var stripped = string.Join('\n', lines
                .SkipWhile(l => l.TrimStart().StartsWith("--") || string.IsNullOrWhiteSpace(l)));

            if (string.IsNullOrWhiteSpace(stripped))
                continue;

            if (!stripped.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) &&
                !stripped.TrimStart().StartsWith("REPLACE", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Skipping non-INSERT statement: {Stmt}",
                    stripped.Length > 80 ? stripped[..80] + "..." : stripped);
                continue;
            }

            // Use INSERT IGNORE to skip collation-level duplicates (e.g. Lesjofors vs LESJÖFORS)
            if (stripped.TrimStart().StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
                stripped = string.Concat("INSERT IGNORE INTO", stripped.AsSpan(stripped.IndexOf("INTO", StringComparison.OrdinalIgnoreCase) + 4));

            await db.Database.ExecuteSqlRawAsync(stripped, ct);
            executed++;
        }

        _logger.LogInformation("Imported {Statements} statements", executed);

        // 4. Rebuild product-manufacturer links based on article codes
        var manufacturers = await db.Manufacturers.AsNoTracking().ToListAsync(ct);
        if (manufacturers.Count == 0)
        {
            _logger.LogWarning("No manufacturers imported, skipping link rebuild");
            return;
        }

        var byPrefix = new Dictionary<string, Manufacturer>(StringComparer.OrdinalIgnoreCase);
        var byName = new Dictionary<string, Manufacturer>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in manufacturers)
        {
            if (!string.IsNullOrWhiteSpace(m.MarketPrefix))
                byPrefix.TryAdd(m.MarketPrefix, m);
            if (!string.IsNullOrWhiteSpace(m.Name))
                byName.TryAdd(m.Name, m);
        }

        const string containsValue = "=";
        var products = await db.Products
            .Where(p => p.Article != null && p.Article.Contains(containsValue))
            .ToListAsync(ct);

        var linked = 0;
        foreach (var product in products)
        {
            var parts = product.Article!.Split('=');
            if (parts.Length <= 1) continue;

            var raw = parts[^1];
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var code = raw.Length >= 3 ? raw[..3] : raw;

            if (byPrefix.TryGetValue(code, out var mfr) || byName.TryGetValue(code, out mfr))
            {
                product.ManufacturerId = mfr.Id;
                linked++;
            }
        }

        if (linked > 0)
            await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Manufacturer link rebuild completed: {Linked}/{Total} products linked",
            linked, products.Count);
    }

    /// <summary>
    /// Splits SQL text into statements by ';' but only outside of string literals,
    /// so semicolons inside values like 'Meat&amp;Doria' are not treated as delimiters.
    /// </summary>
    private static List<string> SplitSqlStatements(string sql)
    {
        var results = new List<string>();
        var inQuote = false;
        var start = 0;

        for (var i = 0; i < sql.Length; i++)
        {
            var ch = sql[i];
            if (ch == '\'')
            {
                if (inQuote && i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    i++; // skip escaped quote ''
                    continue;
                }
                inQuote = !inQuote;
            }
            else if (ch == ';' && !inQuote)
            {
                var stmt = sql[start..i].Trim();
                if (stmt.Length > 0)
                    results.Add(stmt);
                start = i + 1;
            }
        }

        // Last statement (no trailing semicolon)
        var last = sql[start..].Trim();
        if (last.Length > 0)
            results.Add(last);

        return results;
    }
}
