using Elementum.Application.Options;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Outbound.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Elementum.Infrastructure.Outbound.Data.Repositories;

/// <summary>
/// Secondary / Driven Outbound Adapter: Implements <see cref="IPriceHistoryRepository"/>
/// for MySQL persistence and querying via Entity Framework Core.
/// </summary>
public class PriceHistoryRepository : IPriceHistoryRepository
{
    private const int DefaultDailyRollupHour = 22;

    private readonly ElementumDbContext _db;
    private readonly IDailyCandleAggregator _candleAggregator;
    private readonly IPriceHistoryPruner _pruner;
    private readonly int _dailyRollupHour;

    public PriceHistoryRepository(
        ElementumDbContext db,
        IDailyCandleAggregator candleAggregator,
        IPriceHistoryPruner pruner,
        IOptions<WorkerScheduleOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(candleAggregator);
        ArgumentNullException.ThrowIfNull(pruner);

        _db = db;
        _candleAggregator = candleAggregator;
        _pruner = pruner;
        _dailyRollupHour = options?.Value?.DailyRollupHour ?? DefaultDailyRollupHour;
    }

    /// <inheritdoc />
    public async Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalMetalsCount = await _db.Metals.CountAsync(ct);
        if (totalMetalsCount == 0)
            return false;

        var ingestedMetalsCountToday = await _db.PriceHistory
            .Where(x => x.EntryDate == today)
            .Select(x => x.MetalId)
            .Distinct()
            .CountAsync(ct);

        return ingestedMetalsCountToday >= totalMetalsCount;
    }

    /// <inheritdoc />
    public async Task SavePricesAsync(IReadOnlyList<PriceHistory> prices, CancellationToken cancellationToken = default)
    {
        if (prices == null || prices.Count == 0)
            return;

        var candidates = prices
            .Where(p => p is { MetalId: > 0, Price: > 0 })
            .ToList();
        if (candidates.Count == 0)
            return;

        var metalIds = candidates.Select(p => p.MetalId).Distinct().ToList();
        var currencies = candidates.Select(p => p.Currency).Distinct().ToList();
        var timestamps = candidates.Select(p => p.ReferenceTimestamp).Distinct().ToList();

        var existingRows = await _db.PriceHistory
            .Where(x => metalIds.Contains(x.MetalId)
                        && currencies.Contains(x.Currency)
                        && timestamps.Contains(x.ReferenceTimestamp))
            .ToListAsync(cancellationToken);

        var existingByKey = existingRows
            .GroupBy(x => (x.MetalId, x.Currency, x.ReferenceTimestamp))
            .ToDictionary(g => g.Key, g => g.First());
        var isCloseHour = DateTime.UtcNow.Hour >= _dailyRollupHour;

        foreach (var price in candidates)
        {
            var key = (price.MetalId, price.Currency, price.ReferenceTimestamp);
            if (existingByKey.TryGetValue(key, out var existingRow))
            {
                existingRow.UpdatePrices(
                    price: price.Price,
                    highPrice: price.HighPrice,
                    lowPrice: price.LowPrice,
                    openPrice: price.OpenPrice,
                    prevClosePrice: price.PrevClosePrice,
                    ch: price.Ch,
                    chp: price.Chp,
                    referenceTimestamp: price.ReferenceTimestamp,
                    symbol: price.Symbol);
            }
            else
            {
                _db.PriceHistory.Add(price);
                existingByKey[key] = price;
            }

            await _candleAggregator.StageSummaryTickAsync(
                _db, price.MetalId, price.Currency, price.EntryDate, price.Price, 1.0m, isCloseHour, cancellationToken);
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in _db.ChangeTracker.Entries<DailyPriceSummary>())
            {
                await entry.ReloadAsync(cancellationToken);
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Second concurrent modification: state already overwritten by concurrent process, suppress conflict
            }
        }
    }

    /// <inheritdoc />
    public Task AggregateDailySummaryAsync(DateOnly date, CancellationToken ct = default) =>
        _candleAggregator.AggregateDailySummaryAsync(_db, date, ct);

    /// <inheritdoc />
    public Task<int> PruneHourlyDataOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default) =>
        _pruner.PruneHourlyDataOlderThanAsync(_db, thresholdUtc, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyPriceSummary>> GetDailySummariesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default) =>
        await _db.DailyPriceSummaries
            .Include(s => s.Metal)
            .Where(s => s.EntryDate >= fromDate && s.EntryDate <= toDate)
            .OrderBy(s => s.EntryDate)
            .ThenBy(s => s.MetalId)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Metals>> GetMetalsAsync(CancellationToken ct = default) =>
        await _db.Metals.ToListAsync(ct);

    /// <inheritdoc />
    public async Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct) =>
        await _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == symbol)
            .OrderByDescending(x => x.EntryDate)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct)
    {
        var latestByMetal = _db.PriceHistory
            .GroupBy(x => x.MetalId)
            .Select(g => new { MetalId = g.Key, MaxTimestamp = g.Max(x => x.ReferenceTimestamp) });

        return await (
            from p in _db.PriceHistory.Include(x => x.Metal)
            join latest in latestByMetal
                on new { p.MetalId, p.ReferenceTimestamp }
                equals new { latest.MetalId, ReferenceTimestamp = latest.MaxTimestamp }
            select p
        ).ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceHistory>> GetPriceHistoryByMetalSymbolAsync(
        string symbol,
        string? currency = null,
        CancellationToken ct = default)
    {
        var query = _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == symbol);

        query = ApplyCurrencyFilter(query, currency);

        return await query
            .OrderBy(p => p.EntryDate)
            .ThenBy(p => p.ReferenceTimestamp)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<PriceHistory> Items, int TotalCount)> GetPriceHistoryByMetalSymbolAndDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null &&
                        x.Metal.Symbol == symbol &&
                        x.EntryDate >= firstDate &&
                        x.EntryDate <= lastDate);

        query = ApplyCurrencyFilter(query, currency);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.EntryDate)
            .ThenBy(p => p.ReferenceTimestamp)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    private static IQueryable<PriceHistory> ApplyCurrencyFilter(IQueryable<PriceHistory> query, string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return query;

        var cur = currency.Trim().ToUpperInvariant();
        return query.Where(p => p.Currency == cur);
    }
}
