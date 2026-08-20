using System.Globalization;
using Elementum.Domain.Constants;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Infrastructure.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Infrastructure.Data.Repositories;

/// <summary>
/// Secondary / Driven Outbound Adapter: Implements <see cref="IPriceHistoryRepository"/> and <see cref="IElementumDbContext"/>
/// for MySQL persistence and querying via Entity Framework Core.
/// </summary>
public class PriceHistoryRepository : IElementumDbContext
{
    private const int DefaultAggregationHistoryCount = 30;
    private const int RoundingPrecision = 4;
    private const int DaysPerWeekMultiplier = 7;

    private readonly ElementumDbContext _db;
    private readonly IDailyCandleAggregator _candleAggregator;
    private readonly IPriceHistoryPruner _pruner;

    public PriceHistoryRepository(
        ElementumDbContext db,
        IDailyCandleAggregator candleAggregator,
        IPriceHistoryPruner pruner)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(candleAggregator);
        ArgumentNullException.ThrowIfNull(pruner);

        _db = db;
        _candleAggregator = candleAggregator;
        _pruner = pruner;
    }

    /// <inheritdoc />
    public async Task SavePricesAsync(IReadOnlyList<PriceHistory> prices, CancellationToken cancellationToken = default)
    {
        if (prices == null || prices.Count == 0)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isCloseHour = DateTime.UtcNow.Hour >= 22;

        foreach (var price in prices)
        {
            if (price == null || price.MetalId <= 0 || price.Price <= 0)
                continue;

            var existingRow = await _db.PriceHistory
                .FirstOrDefaultAsync(x => x.MetalId == price.MetalId && x.Currency == price.Currency && x.ReferenceTimestamp == price.ReferenceTimestamp, cancellationToken);

            if (existingRow != null)
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
            }

            // Real-time update of DailyPriceSummary candle
            await _candleAggregator.UpdateSummaryForTickAsync(
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
            await _db.SaveChangesAsync(cancellationToken);
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

    private Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct) =>
        _db.Metals.FirstOrDefaultAsync(x => x.Symbol == symbol, ct);

    /// <inheritdoc />
    public async Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct) =>
        await _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == symbol)
            .OrderByDescending(x => x.EntryDate)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct)
    {
        var latestIds = await _db.PriceHistory
            .GroupBy(x => x.MetalId)
            .Select(g => g.Max(x => x.Id))
            .ToListAsync(ct);

        if (latestIds.Count == 0)
            return [];

        return await _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => latestIds.Contains(x.Id))
            .ToListAsync(ct);
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
    public async Task<IReadOnlyList<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency = null,
        CancellationToken ct = default)
    {
        var query = _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null &&
                        x.Metal.Symbol == symbol &&
                        x.EntryDate >= firstDate &&
                        x.EntryDate <= lastDate);

        query = ApplyCurrencyFilter(query, currency);

        return await query
            .OrderBy(p => p.ReferenceTimestamp)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);
    }

    private static IQueryable<PriceHistory> ApplyCurrencyFilter(IQueryable<PriceHistory> query, string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return query;

        var cur = currency.Trim().ToUpperInvariant();
        return query.Where(p => p.Currency == cur);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct)
    {
        var metal = await GetMetalBySymbol(metalSymbol, ct);
        if (metal == null)
            return [];

        var effectiveCount = count <= 0 ? DefaultAggregationHistoryCount : count;
        var agg = (aggregation ?? "daily").Trim().ToLowerInvariant();

        if (agg == "daily")
        {
            var dailyRows = await _db.PriceHistory
                .Include(x => x.Metal)
                .Where(x => x.MetalId == metal.Id)
                .OrderByDescending(x => x.EntryDate)
                .Take(effectiveCount)
                .ToListAsync(ct);

            dailyRows.Reverse();
            return dailyRows;
        }
        else if (agg == "monthly")
        {
            var monthGroups = await _db.PriceHistory
                .Where(x => x.MetalId == metal.Id)
                .GroupBy(x => new { x.EntryDate.Year, x.EntryDate.Month })
                .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                .Take(effectiveCount)
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    AvgPrice = g.Average(p => p.Price),
                    Currency = g.Max(p => p.Currency)
                })
                .ToListAsync(ct);

            monthGroups.Reverse();

            return monthGroups.Select(m => PriceHistory.Create(
                metalId: metal.Id,
                currency: m.Currency ?? DomainConstants.Currencies.Usd,
                entryDate: new DateOnly(m.Year, m.Month, 1),
                price: Math.Round(m.AvgPrice, RoundingPrecision, MidpointRounding.ToEven),
                symbol: metalSymbol,
                metal: metal)).ToList();
        }
        else if (agg == "yearly")
        {
            var yearGroups = await _db.PriceHistory
                .Where(x => x.MetalId == metal.Id)
                .GroupBy(x => x.EntryDate.Year)
                .OrderByDescending(g => g.Key)
                .Take(effectiveCount)
                .Select(g => new
                {
                    Year = g.Key,
                    AvgPrice = g.Average(p => p.Price),
                    Currency = g.Max(p => p.Currency)
                })
                .ToListAsync(ct);

            yearGroups.Reverse();

            return yearGroups.Select(y => PriceHistory.Create(
                metalId: metal.Id,
                currency: y.Currency ?? DomainConstants.Currencies.Usd,
                entryDate: new DateOnly(y.Year, 1, 1),
                price: Math.Round(y.AvgPrice, RoundingPrecision, MidpointRounding.ToEven),
                symbol: metalSymbol,
                metal: metal)).ToList();
        }
        else // weekly
        {
            var recentRows = await _db.PriceHistory
                .Where(x => x.MetalId == metal.Id)
                .OrderByDescending(x => x.EntryDate)
                .Take(effectiveCount * DaysPerWeekMultiplier)
                .ToListAsync(ct);

            recentRows.Reverse();

            var dfi = DateTimeFormatInfo.CurrentInfo;
            var cal = dfi.Calendar;

            var weeklyGroups = recentRows
                .GroupBy(x =>
                {
                    var dt = x.EntryDate.ToDateTime(TimeOnly.MinValue);
                    var week = cal.GetWeekOfYear(dt, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
                    return (x.EntryDate.Year, Week: week);
                })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week)
                .TakeLast(effectiveCount)
                .Select(g => PriceHistory.Create(
                    metalId: metal.Id,
                    currency: g.First().Currency,
                    entryDate: g.First().EntryDate,
                    price: Math.Round(g.Average(p => p.Price), RoundingPrecision, MidpointRounding.ToEven),
                    symbol: metalSymbol,
                    metal: metal))
                .ToList();

            return weeklyGroups;
        }
    }
}
