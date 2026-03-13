using System.Globalization;
using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Shared.DTOs;
using Elementum.Shared.Mapping;
using Elementum.Shared.Objects;
using Microsoft.EntityFrameworkCore;


namespace Elementum.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for Elementum. Maps to the existing MySQL schema with tables <c>metals</c> and <c>price_history</c>.
/// Used by the Worker (ingestion) and the ServiceApi (read API).
/// </summary>
public class ElementumDbContext : DbContext, IElementumDbContext
{
    /// <summary>Initializes the context with the given options (e.g. connection string, provider).</summary>
    public ElementumDbContext(DbContextOptions<ElementumDbContext> options)
        : base(options)
    {
    }

    #region SET

    /// <summary>DbSet for the <c>metals</c> table.</summary>
    public DbSet<Metals> Metals => Set<Metals>();

    /// <summary>DbSet for the <c>price_history</c> table.</summary>
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();

    #endregion SET

    #region GET

    public async Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await PriceHistory.AnyAsync(x => x.EntryDate == today, ct);
    }

    #region Metals

    public async Task<IEnumerable<Metals>> GetMetalsAll(CancellationToken ct)
    {
        return await Metals.ToListAsync(ct);
    }

    public async Task<Metals?> GetMetalById(int id, CancellationToken ct)
    {
        return await Metals.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct)
    {
        return await Metals.FirstOrDefaultAsync(x => x.Symbol == symbol, ct);
    }

    #endregion Metals

    #region PriceHistory

    public async Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct)
    {
        return await PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == symbol)
            .OrderByDescending(x => x.EntryDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAll(CancellationToken ct)
    {
        return await PriceHistory
            .Include(x => x.Metal)
            .ToListAsync(ct);
    }

    /// <summary>Latest price history entry per metal. Groups in memory so Include(Metal) is preserved for mapping.</summary>
    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken ct)
    {
        var all = await PriceHistory
            .Include(x => x.Metal)
            .ToListAsync(ct);

        var latestPerMetal = all
            .GroupBy(x => x.MetalId)
            .Select(g => g.OrderByDescending(x => x.EntryDate).First())
            .ToList();

        return latestPerMetal.Select(PriceHistoryMapping.ToPriceHistoryDto);
    }

    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken ct)
    {
        return await PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == symbol)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate, CancellationToken ct)
    {
        return await PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.EntryDate >= firstDate &&
                        x.EntryDate <= lastDate)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct)
    {
        return await PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null &&
                        x.Metal.Symbol == symbol &&
                        x.EntryDate >= firstDate &&
                        x.EntryDate <= lastDate)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns price history for a metal: either last N daily points (count) or aggregated by period (weekly/monthly/yearly)
    /// with one value per period as the average of all days in that period.
    /// Count is passed by the caller (e.g. 31 daily, 52 weekly, 12 monthly, 10 yearly); API returns at most that many entries.
    /// </summary>
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct)
    {
        var metal = await GetMetalBySymbol(metalSymbol, ct);
        if (metal == null)
            return Array.Empty<PriceHistory>();

        var period = aggregation.Trim().ToLowerInvariant() switch
        {
            "daily" => 0,
            "weekly" => 1,
            "monthly" => 2,
            "yearly" => 3,
            _ => 0
        };

        // Caller passes desired count (e.g. PeriodCounts: Daily 31, Weekly 52, Monthly 12, Yearly 10). Fallback only if count <= 0.
        int effectiveCount = count > 0 ? count : 31;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (period == 0)
        {
            // Daily: last N days, no aggregation
            return await PriceHistory
                .Include(x => x.Metal)
                .Where(x => x.Metal != null && x.Metal.Symbol == metalSymbol)
                .OrderByDescending(x => x.EntryDate)
                .Take(effectiveCount)
                .ToListAsync(ct);
        }

        // Aggregated: need enough date range, then group and average
        DateOnly fromDate = period switch
        {
            1 => today.AddDays(-effectiveCount * 7 - 7),
            2 => today.AddMonths(-effectiveCount - 1),
            3 => today.AddYears(-effectiveCount - 1),
            _ => today.AddDays(-effectiveCount)
        };

        var raw = await PriceHistory
            .Include(x => x.Metal)
            .Where(x => x.Metal != null && x.Metal.Symbol == metalSymbol && x.EntryDate >= fromDate && x.EntryDate <= today)
            .OrderBy(x => x.EntryDate)
            .ToListAsync(ct);

        if (raw.Count == 0)
            return Array.Empty<PriceHistory>();

        List<PriceHistory> result;
        if (period == 1)
        {
            var weekGroups = raw
                .GroupBy(x => (ISOWeek.GetYear(x.EntryDate), ISOWeek.GetWeekOfYear(x.EntryDate)))
                .OrderBy(g => g.Key.Item1).ThenBy(g => g.Key.Item2)
                .Select(g => new
                {
                    Key = g.Key,
                    AvgPrice = g.Average(p => p.Price),
                    First = g.OrderBy(p => p.EntryDate).First()
                })
                .TakeLast(effectiveCount)
                .ToList();
            result = weekGroups.Select(w => new PriceHistory
            {
                Id = 0,
                MetalId = metal.Id,
                Currency = w.First.Currency,
                Symbol = metalSymbol,
                EntryDate = ISOWeek.ToDateOnly(w.Key.Item1, w.Key.Item2, DayOfWeek.Monday),
                Price = w.AvgPrice,
                Metal = metal
            }).ToList();
        }
        else if (period == 2)
        {
            var monthGroups = raw
                .GroupBy(x => (x.EntryDate.Year, x.EntryDate.Month))
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Key = g.Key,
                    AvgPrice = g.Average(p => p.Price),
                    First = g.First()
                })
                .TakeLast(effectiveCount)
                .ToList();
            result = monthGroups.Select(m => new PriceHistory
            {
                Id = 0,
                MetalId = metal.Id,
                Currency = m.First.Currency,
                Symbol = metalSymbol,
                EntryDate = new DateOnly(m.Key.Year, m.Key.Month, 1),
                Price = m.AvgPrice,
                Metal = metal
            }).ToList();
        }
        else
        {
            var yearGroups = raw
                .GroupBy(x => x.EntryDate.Year)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Year = g.Key,
                    AvgPrice = g.Average(p => p.Price),
                    First = g.First()
                })
                .TakeLast(effectiveCount)
                .ToList();
            result = yearGroups.Select(y => new PriceHistory
            {
                Id = 0,
                MetalId = metal.Id,
                Currency = y.First.Currency,
                Symbol = metalSymbol,
                EntryDate = new DateOnly(y.Year, 1, 1),
                Price = y.AvgPrice,
                Metal = metal
            }).ToList();
        }

        return result;
    }

    #endregion PriceHistory
    #endregion GET

    #region CREATING

    /// <summary>Configures the entity model: table names, keys, and column mappings (snake_case for MySQL).</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // metals: table name and column renames to match existing schema
        modelBuilder.Entity<Metals>(e =>
        {
            e.ToTable("metals");
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // price_history: table name, FK to metals (cascade delete), snake_case column names
        modelBuilder.Entity<PriceHistory>(e =>
        {
            e.ToTable("price_history");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Metal).WithMany().HasForeignKey(x => x.MetalId).OnDelete(DeleteBehavior.Cascade);

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
    }

   


    #endregion CREATING
}
