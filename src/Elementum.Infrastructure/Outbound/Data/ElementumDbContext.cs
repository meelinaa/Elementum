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
                existingRow.Exchange = api.Exchange;
                existingRow.Symbol = api.Symbol;
                existingRow.ReferenceTimestamp = api.Timestamp.ToString(CultureInfo.InvariantCulture);
                existingRow.OpenTime = api.OpenTime.ToString(CultureInfo.InvariantCulture);
                existingRow.Price = api.Price;
                existingRow.PrevClosePrice = api.PrevClosePrice;
                existingRow.OpenPrice = api.OpenPrice;
                existingRow.LowPrice = api.LowPrice;
                existingRow.HighPrice = api.HighPrice;
                existingRow.Ch = api.Ch;
                existingRow.Chp = api.Chp;
                existingRow.Ask = api.Ask;
                existingRow.Bid = api.Bid;
                existingRow.PriceGram24k = api.PriceGram24k;
                existingRow.PriceGram22k = api.PriceGram22k;
                existingRow.PriceGram21k = api.PriceGram21k;
                existingRow.PriceGram20k = api.PriceGram20k;
                existingRow.PriceGram18k = api.PriceGram18k;
                existingRow.PriceGram16k = api.PriceGram16k;
                existingRow.PriceGram14k = api.PriceGram14k;
                existingRow.PriceGram10k = api.PriceGram10k;
            }
            else
            {
                var row = new PriceHistory
                {
                    MetalId = metal.Id,
                    Currency = currency,
                    Exchange = api.Exchange,
                    Symbol = api.Symbol,
                    ReferenceTimestamp = api.Timestamp.ToString(CultureInfo.InvariantCulture),
                    OpenTime = api.OpenTime.ToString(CultureInfo.InvariantCulture),
                    EntryDate = today,
                    Price = api.Price,
                    PrevClosePrice = api.PrevClosePrice,
                    OpenPrice = api.OpenPrice,
                    LowPrice = api.LowPrice,
                    HighPrice = api.HighPrice,
                    Ch = api.Ch,
                    Chp = api.Chp,
                    Ask = api.Ask,
                    Bid = api.Bid,
                    PriceGram24k = api.PriceGram24k,
                    PriceGram22k = api.PriceGram22k,
                    PriceGram21k = api.PriceGram21k,
                    PriceGram20k = api.PriceGram20k,
                    PriceGram18k = api.PriceGram18k,
                    PriceGram16k = api.PriceGram16k,
                    PriceGram14k = api.PriceGram14k,
                    PriceGram10k = api.PriceGram10k
                };
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
    }
}
