using System.Globalization;
using Elementum.Domain.Entities;
using Elementum.Domain.Models;
using Elementum.Infrastructure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for Elementum. Maps to the MySQL schema with tables <c>metals</c> and <c>price_history</c>.
/// Implements <see cref="IElementumDbContext"/> and domain port <see cref="Elementum.Domain.Ports.IPriceHistoryRepository"/>.
/// </summary>
public class ElementumDbContext(DbContextOptions<ElementumDbContext> options) : DbContext(options), IElementumDbContext
{
    /// <summary>DbSet for the <c>metals</c> table.</summary>
    public DbSet<Metals> Metals => Set<Metals>();

    /// <summary>DbSet for the <c>price_history</c> table.</summary>
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();

    /// <summary>DbSet for distributed locking table.</summary>
    public DbSet<DistributedLockEntity> DistributedLocks => Set<DistributedLockEntity>();

    /// <summary>DbSet for consolidated daily price summaries / candles (22:00 Close, Min, Max, Open).</summary>
    public DbSet<DailyPriceSummary> DailyPriceSummaries => Set<DailyPriceSummary>();

    /// <summary>Returns true if all configured metals in the catalog have price history entries for today (UTC).</summary>
    public async Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalMetalsCount = await Metals.CountAsync(ct);
        if (totalMetalsCount == 0)
            return false;

        var ingestedMetalsCountToday = await PriceHistory
            .Where(x => x.EntryDate == today)
            .Select(x => x.MetalId)
            .Distinct()
            .CountAsync(ct);

        return ingestedMetalsCountToday >= totalMetalsCount;
    }

    /// <summary>Saves hourly quotes for all metals in USD & EUR from Edelmetalle API.</summary>
    public async Task SaveEdelmetallePricesAsync(EdelmetalleApiResponse data, CancellationToken ct = default)
    {
        if (data == null)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isCloseHour = DateTime.UtcNow.Hour >= 22;
        var allMetals = await Metals.ToListAsync(ct);

        (string Symbol, string Name, decimal UsdPrice, decimal EurPrice)[] quotes =
        [
            ("XAU", "Gold", data.GoldUsd, data.GoldEur),
            ("XAG", "Silver", data.SilberUsd, data.SilberEur),
            ("XPT", "Platinum", data.PlatinUsd, data.PlatinEur),
            ("XPD", "Palladium", data.PalladiumUsd, data.PalladiumEur)
        ];

        foreach (var q in quotes)
        {
            var metal = allMetals.FirstOrDefault(m =>
                string.Equals(m.Symbol, q.Symbol, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Name, q.Name, StringComparison.OrdinalIgnoreCase));

            if (metal == null)
                continue;

            // Ingest USD and EUR price ticks
            (string Currency, decimal Price)[] currencyQuotes = [("USD", q.UsdPrice), ("EUR", q.EurPrice)];

            foreach (var (currency, price) in currencyQuotes)
            {
                if (price <= 0)
                    continue;

                // 1. Add raw hourly tick
                var tick = Elementum.Domain.Entities.PriceHistory.Create(
                    metalId: metal.Id,
                    currency: currency,
                    entryDate: today,
                    price: price,
                    symbol: $"{q.Symbol}{currency}",
                    referenceTimestamp: data.Timestamp.ToString(CultureInfo.InvariantCulture),
                    openTime: DateTime.UtcNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture));

                PriceHistory.Add(tick);

                // 2. Real-time update of DailyPriceSummary candle
                var dailySummary = await DailyPriceSummaries
                    .FirstOrDefaultAsync(s => s.MetalId == metal.Id && s.Currency == currency && s.EntryDate == today, ct);

                if (dailySummary == null)
                {
                    dailySummary = DailyPriceSummary.Create(
                        metalId: metal.Id,
                        currency: currency,
                        entryDate: today,
                        openPrice: price,
                        highPrice: price,
                        lowPrice: price,
                        closePrice: price,
                        exchangeRateUsdEur: data.WechselkursUsdEur);

                    DailyPriceSummaries.Add(dailySummary);
                }
                else
                {
                    dailySummary.ApplyPriceTick(price, data.WechselkursUsdEur, isClosePrice: isCloseHour);
                }
            }
        }

        await SaveChangesAsync(ct);
    }

    /// <summary>Aggregates the daily candle (Open, High, Low, Close at 22:00) into daily_price_summaries for the given date.</summary>
    public async Task AggregateDailySummaryAsync(DateOnly date, CancellationToken ct = default)
    {
        var dayTicks = await PriceHistory
            .Where(p => p.EntryDate == date)
            .OrderBy(p => p.ReferenceTimestamp)
            .ToListAsync(ct);

        if (dayTicks.Count == 0)
            return;

        var groups = dayTicks.GroupBy(p => new { p.MetalId, p.Currency });

        foreach (var g in groups)
        {
            var openPrice = g.First().Price;
            var highPrice = g.Max(p => p.Price);
            var lowPrice = g.Min(p => p.Price);
            var closePrice = g.Last().Price;

            var existing = await DailyPriceSummaries
                .FirstOrDefaultAsync(s => s.MetalId == g.Key.MetalId && s.Currency == g.Key.Currency && s.EntryDate == date, ct);

            if (existing == null)
            {
                var summary = DailyPriceSummary.Create(
                    metalId: g.Key.MetalId,
                    currency: g.Key.Currency,
                    entryDate: date,
                    openPrice: openPrice,
                    highPrice: highPrice,
                    lowPrice: lowPrice,
                    closePrice: closePrice);

                DailyPriceSummaries.Add(summary);
            }
            else
            {
                existing.OpenPrice = openPrice;
                existing.HighPrice = highPrice;
                existing.LowPrice = lowPrice;
                existing.ClosePrice = closePrice;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await SaveChangesAsync(ct);
    }

    /// <summary>Deletes hourly price_history records older than the specified UTC timestamp (7-day retention policy).</summary>
    public async Task<int> PruneHourlyDataOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default)
    {
        var thresholdDate = DateOnly.FromDateTime(thresholdUtc);

        var staleRecords = await PriceHistory
            .Where(p => p.EntryDate < thresholdDate)
            .ToListAsync(ct);

        if (staleRecords.Count == 0)
            return 0;

        PriceHistory.RemoveRange(staleRecords);
        await SaveChangesAsync(ct);
        return staleRecords.Count;
    }

    /// <summary>Retrieves daily candle summaries (Min/Max/Open/Close) for a metal symbol and currency in a date range.</summary>
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

        var query = DailyPriceSummaries
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

    /// <summary>Saves incoming daily prices from external sources to the database using idempotent upsert.</summary>
    public async Task SavePricesAsync(IReadOnlyList<DailyPrices> prices, CancellationToken cancellationToken = default)
    {
        if (prices.Count == 0)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var metalsBySymbol = await Metals.ToDictionaryAsync(m => m.Symbol, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var api in prices)
        {
            if (string.IsNullOrEmpty(api.Metal) || !metalsBySymbol.TryGetValue(api.Metal, out var metal))
            {
                continue;
            }

            var currency = string.IsNullOrEmpty(api.Currency) ? "USD" : api.Currency;

            var existingRow = await PriceHistory
                .FirstOrDefaultAsync(x => x.MetalId == metal.Id && x.Currency == currency && x.EntryDate == today, cancellationToken);

            if (existingRow != null)
            {
                existingRow.UpdatePrices(
                    price: api.Price,
                    highPrice: api.HighPrice,
                    lowPrice: api.LowPrice,
                    openPrice: api.OpenPrice,
                    prevClosePrice: api.PrevClosePrice,
                    ask: api.Ask,
                    bid: api.Bid,
                    ch: api.Ch,
                    chp: api.Chp,
                    priceGram24k: api.PriceGram24k,
                    priceGram22k: api.PriceGram22k,
                    priceGram21k: api.PriceGram21k,
                    priceGram20k: api.PriceGram20k,
                    priceGram18k: api.PriceGram18k,
                    priceGram16k: api.PriceGram16k,
                    priceGram14k: api.PriceGram14k,
                    priceGram10k: api.PriceGram10k,
                    referenceTimestamp: api.Timestamp.ToString(CultureInfo.InvariantCulture),
                    openTime: api.OpenTime.ToString(CultureInfo.InvariantCulture),
                    exchange: api.Exchange,
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
                    exchange: api.Exchange,
                    openPrice: api.OpenPrice,
                    highPrice: api.HighPrice,
                    lowPrice: api.LowPrice,
                    prevClosePrice: api.PrevClosePrice,
                    ask: api.Ask,
                    bid: api.Bid,
                    ch: api.Ch,
                    chp: api.Chp,
                    priceGram24k: api.PriceGram24k,
                    priceGram22k: api.PriceGram22k,
                    priceGram21k: api.PriceGram21k,
                    priceGram20k: api.PriceGram20k,
                    priceGram18k: api.PriceGram18k,
                    priceGram16k: api.PriceGram16k,
                    priceGram14k: api.PriceGram14k,
                    priceGram10k: api.PriceGram10k,
                    referenceTimestamp: api.Timestamp.ToString(CultureInfo.InvariantCulture),
                    openTime: api.OpenTime.ToString(CultureInfo.InvariantCulture));

                PriceHistory.Add(row);
            }
        }

        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IQueryable<Metals> QueryMetals() => Metals;

    /// <summary>Returns a metal by primary key, or null if not found.</summary>
    public async Task<Metals?> GetMetalById(int id, CancellationToken ct)
    {
        return await Metals.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    /// <summary>Returns a metal by symbol (e.g. XAU, XAG), or null if not found.</summary>
    public async Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct)
    {
        return await Metals.FirstOrDefaultAsync(x => x.Symbol == symbol, ct);
    }

    /// <summary>Returns the most recent price history row for the given metal symbol (by EntryDate desc).</summary>
    public async Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct)
    {
        return await PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == symbol)
            .OrderByDescending(x => x.EntryDate)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryAll() =>
        PriceHistory.Include(x => x.Metal);

    /// <summary>Latest price history entry per metal. Evaluated on database server side to avoid in-memory table scans.</summary>
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct)
    {
        var latestIds = await PriceHistory
            .GroupBy(x => x.MetalId)
            .Select(g => g.Max(x => x.Id))
            .ToListAsync(ct);

        if (latestIds.Count == 0)
            return [];

        return await PriceHistory
            .Include(x => x.Metal)
            .Where(x => latestIds.Contains(x.Id))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbol(string symbol) =>
        PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == symbol);

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate) =>
        PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.EntryDate >= firstDate && x.EntryDate <= lastDate);

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate) =>
        PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null &&
                        x.Metal.Symbol == symbol &&
                        x.EntryDate >= firstDate &&
                        x.EntryDate <= lastDate);

    /// <summary>
    /// Returns price history for a metal: either last N daily points (count) or aggregated by period (weekly/monthly/yearly).
    /// Uses bounded queries and server-side aggregation to avoid unbounded in-memory table scans.
    /// </summary>
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct)
    {
        var metal = await GetMetalBySymbol(metalSymbol, ct);
        if (metal == null)
            return [];

        var effectiveCount = count <= 0 ? 30 : count;
        var agg = (aggregation ?? "daily").Trim().ToLowerInvariant();

        if (agg == "daily")
        {
            var dailyRows = await PriceHistory
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
            var monthGroups = await PriceHistory
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
                Currency = m.Currency ?? "USD",
                Symbol = metalSymbol,
                EntryDate = new DateOnly(m.Year, m.Month, 1),
                Price = Math.Round(m.AvgPrice, 4, MidpointRounding.ToEven),
                Metal = metal
            }).ToList();
        }
        else if (agg == "yearly")
        {
            var yearGroups = await PriceHistory
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
                Currency = y.Currency ?? "USD",
                Symbol = metalSymbol,
                EntryDate = new DateOnly(y.Year, 1, 1),
                Price = Math.Round(y.AvgPrice, 4, MidpointRounding.ToEven),
                Metal = metal
            }).ToList();
        }
        else // weekly
        {
            var recentRows = await PriceHistory
                .Where(x => x.MetalId == metal.Id)
                .OrderByDescending(x => x.EntryDate)
                .Take(effectiveCount * 7)
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
                    Price = Math.Round(g.Average(p => p.Price), 4, MidpointRounding.ToEven),
                    Metal = metal
                })
                .ToList();

            return weeklyGroups;
        }
    }

    /// <summary>Configures the entity model: table names, keys, and column mappings.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Metals>(e =>
        {
            e.ToTable("metals");
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<PriceHistory>(e =>
        {
            e.ToTable("price_history");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Metal).WithMany(m => m.PriceHistory).HasForeignKey(x => x.MetalId).OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.MetalId).HasColumnName("metal_id");
            e.Property(x => x.ReferenceTimestamp).HasColumnName("reference_timestamp");
            e.Property(x => x.OpenTime).HasColumnName("open_time");
            e.Property(x => x.EntryDate).HasColumnName("entry_date");
            e.Property(x => x.PrevClosePrice).HasColumnName("prev_close_price");
            e.Property(x => x.OpenPrice).HasColumnName("open_price");
            e.Property(x => x.LowPrice).HasColumnName("low_price");
            e.Property(x => x.HighPrice).HasColumnName("high_price");
            e.Property(x => x.PriceGram24k).HasColumnName("price_gram_24k");
            e.Property(x => x.PriceGram22k).HasColumnName("price_gram_22k");
            e.Property(x => x.PriceGram21k).HasColumnName("price_gram_21k");
            e.Property(x => x.PriceGram20k).HasColumnName("price_gram_20k");
            e.Property(x => x.PriceGram18k).HasColumnName("price_gram_18k");
            e.Property(x => x.PriceGram16k).HasColumnName("price_gram_16k");
            e.Property(x => x.PriceGram14k).HasColumnName("price_gram_14k");
            e.Property(x => x.PriceGram10k).HasColumnName("price_gram_10k");
        });

        modelBuilder.Entity<DistributedLockEntity>(e =>
        {
            e.ToTable("distributed_locks");
            e.HasKey(x => x.Resource);
            e.Property(x => x.Resource).HasColumnName("resource").HasMaxLength(128).IsRequired();
            e.Property(x => x.AcquiredBy).HasColumnName("acquired_by").HasMaxLength(128).IsRequired();
            e.Property(x => x.AcquiredAtUtc).HasColumnName("acquired_at_utc").IsRequired();
            e.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").IsConcurrencyToken().IsRequired();
        });

        modelBuilder.Entity<DailyPriceSummary>(e =>
        {
            e.ToTable("daily_price_summaries");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MetalId, x.Currency, x.EntryDate }).IsUnique();
            e.HasOne(x => x.Metal).WithMany(m => m.DailySummaries).HasForeignKey(x => x.MetalId).OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.MetalId).HasColumnName("metal_id").IsRequired();
            e.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
            e.Property(x => x.EntryDate).HasColumnName("entry_date").IsRequired();
            e.Property(x => x.OpenPrice).HasColumnName("open_price").HasPrecision(18, 4).IsRequired();
            e.Property(x => x.HighPrice).HasColumnName("high_price").HasPrecision(18, 4).IsRequired();
            e.Property(x => x.LowPrice).HasColumnName("low_price").HasPrecision(18, 4).IsRequired();
            e.Property(x => x.ClosePrice).HasColumnName("close_price").HasPrecision(18, 4).IsRequired();
            e.Property(x => x.ExchangeRateUsdEur).HasColumnName("exchange_rate_usd_eur").HasPrecision(18, 8);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        });
    }
}
