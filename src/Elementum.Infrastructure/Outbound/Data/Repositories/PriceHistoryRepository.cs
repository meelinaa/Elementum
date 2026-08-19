using System.Globalization;
using Elementum.Domain.Constants;
using Elementum.Domain.Entities;
using Elementum.Domain.Models;
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
    public async Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct)
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
    public async Task SaveEdelmetallePricesAsync(EdelmetalleApiResponse data, CancellationToken ct = default)
    {
        if (data == null)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isCloseHour = DateTime.UtcNow.Hour >= 22;
        var allMetals = await _db.Metals.ToListAsync(ct);

        (string Symbol, string Name, decimal UsdPrice, decimal EurPrice)[] quotes =
        [
            (DomainConstants.Symbols.Gold, DomainConstants.Names.Gold, data.GoldUsd, data.GoldEur),
            (DomainConstants.Symbols.Silver, DomainConstants.Names.Silver, data.SilberUsd, data.SilberEur),
            (DomainConstants.Symbols.Platinum, DomainConstants.Names.Platinum, data.PlatinUsd, data.PlatinEur),
            (DomainConstants.Symbols.Palladium, DomainConstants.Names.Palladium, data.PalladiumUsd, data.PalladiumEur)
        ];

        foreach (var q in quotes)
        {
            var metal = allMetals.FirstOrDefault(m =>
                string.Equals(m.Symbol, q.Symbol, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Name, q.Name, StringComparison.OrdinalIgnoreCase));

            if (metal == null)
                continue;

            (string Currency, decimal Price)[] currencyQuotes =
            [
                (DomainConstants.Currencies.Usd, q.UsdPrice),
                (DomainConstants.Currencies.Eur, q.EurPrice)
            ];

            var refTimestamp = data.Timestamp;

            foreach (var (currency, price) in currencyQuotes)
            {
                if (price <= 0)
                    continue;

                // 1. Add raw hourly tick if not already present (idempotent insert)
                var tickExists = await _db.PriceHistory.AnyAsync(
                    p => p.MetalId == metal.Id && p.Currency == currency && p.ReferenceTimestamp == refTimestamp, ct);

                if (!tickExists)
                {
                    var tick = Elementum.Domain.Entities.PriceHistory.Create(
                        metalId: metal.Id,
                        currency: currency,
                        entryDate: today,
                        price: price,
                        symbol: $"{q.Symbol}{currency}",
                        referenceTimestamp: refTimestamp);

                    _db.PriceHistory.Add(tick);
                }

                // 2. Real-time update of DailyPriceSummary candle
                await _candleAggregator.UpdateSummaryForTickAsync(
                    _db, metal.Id, currency, today, price, data.WechselkursUsdEur, isCloseHour, ct);
            }
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in _db.ChangeTracker.Entries<DailyPriceSummary>())
            {
                await entry.ReloadAsync(ct);
            }
            await _db.SaveChangesAsync(ct);
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
        string symbol,
        string currency,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        var metal = await GetMetalBySymbol(symbol, ct);
        if (metal == null)
            return Array.Empty<DailyPriceSummary>();

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        var query = _db.DailyPriceSummaries
            .Include(s => s.Metal)
            .Where(s => s.MetalId == metal.Id && s.Currency == normalizedCurrency);

        if (fromDate.HasValue)
            query = query.Where(s => s.EntryDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.EntryDate <= toDate.Value);

        return await query
            .OrderBy(s => s.EntryDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task SavePricesAsync(IReadOnlyList<DailyPrices> prices, CancellationToken cancellationToken = default)
    {
        if (prices.Count == 0)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var metalsBySymbol = await _db.Metals.ToDictionaryAsync(m => m.Symbol, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var api in prices)
        {
            if (string.IsNullOrEmpty(api.Metal) || !metalsBySymbol.TryGetValue(api.Metal, out var metal))
            {
                continue;
            }

            var currency = string.IsNullOrEmpty(api.Currency) ? DomainConstants.Currencies.Usd : api.Currency;

            var existingRow = await _db.PriceHistory
                .FirstOrDefaultAsync(x => x.MetalId == metal.Id && x.Currency == currency && x.EntryDate == today, cancellationToken);

            if (existingRow != null)
            {
                existingRow.UpdatePrices(
                    price: api.Price,
                    highPrice: api.HighPrice,
                    lowPrice: api.LowPrice,
                    openPrice: api.OpenPrice,
                    prevClosePrice: api.PrevClosePrice,
                    ch: api.Ch,
                    chp: api.Chp,
                    referenceTimestamp: api.Timestamp,
                    symbol: api.Symbol);
            }
            else
            {
                var row = Elementum.Domain.Entities.PriceHistory.Create(
                    metalId: metal.Id,
                    currency: currency,
                    entryDate: today,
                    price: api.Price,
                    symbol: api.Symbol,
                    openPrice: api.OpenPrice,
                    highPrice: api.HighPrice,
                    lowPrice: api.LowPrice,
                    prevClosePrice: api.PrevClosePrice,
                    ch: api.Ch,
                    chp: api.Chp,
                    referenceTimestamp: api.Timestamp);

                _db.PriceHistory.Add(row);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IQueryable<Metals> QueryMetals() => _db.Metals;

    /// <inheritdoc />
    public async Task<Metals?> GetMetalById(int id, CancellationToken ct) =>
        await _db.Metals.FirstOrDefaultAsync(x => x.Id == id, ct);

    /// <inheritdoc />
    public async Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct) =>
        await _db.Metals.FirstOrDefaultAsync(x => x.Symbol == symbol, ct);

    /// <inheritdoc />
    public async Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct) =>
        await _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == symbol)
            .OrderByDescending(x => x.EntryDate)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryAll() =>
        _db.PriceHistory.Include(x => x.Metal);

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
    public IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbol(string symbol) =>
        _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == symbol);

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate) =>
        _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.EntryDate >= firstDate && x.EntryDate <= lastDate);

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate) =>
        _db.PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null &&
                        x.Metal.Symbol == symbol &&
                        x.EntryDate >= firstDate &&
                        x.EntryDate <= lastDate);

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

            return monthGroups.Select(m => new PriceHistory
            {
                Id = 0,
                MetalId = metal.Id,
                Currency = m.Currency ?? DomainConstants.Currencies.Usd,
                Symbol = metalSymbol,
                EntryDate = new DateOnly(m.Year, m.Month, 1),
                Price = Math.Round(m.AvgPrice, RoundingPrecision, MidpointRounding.ToEven),
                Metal = metal
            }).ToList();
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

            return yearGroups.Select(y => new PriceHistory
            {
                Id = 0,
                MetalId = metal.Id,
                Currency = y.Currency ?? DomainConstants.Currencies.Usd,
                Symbol = metalSymbol,
                EntryDate = new DateOnly(y.Year, 1, 1),
                Price = Math.Round(y.AvgPrice, RoundingPrecision, MidpointRounding.ToEven),
                Metal = metal
            }).ToList();
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
                .Select(g => new PriceHistory
                {
                    Id = 0,
                    MetalId = metal.Id,
                    Currency = g.First().Currency,
                    Symbol = metalSymbol,
                    EntryDate = g.First().EntryDate,
                    Price = Math.Round(g.Average(p => p.Price), RoundingPrecision, MidpointRounding.ToEven),
                    Metal = metal
                })
                .ToList();

            return weeklyGroups;
        }
    }
}
