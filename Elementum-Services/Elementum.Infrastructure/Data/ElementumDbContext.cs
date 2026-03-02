using Elementum.Shared.Objects;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for Elementum. Maps to the existing MySQL schema with tables <c>metals</c> and <c>price_history</c>.
/// Used by the Worker (ingestion) and the ServiceApi (read API).
/// </summary>
public class ElementumDbContext : DbContext
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

    /// <summary>
    /// Returns whether any row in <c>price_history</c> has <see cref="PriceHistory.EntryDate"/> equal to today (UTC).
    /// Used by the ingestion job to avoid duplicate daily runs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if at least one such row exists; otherwise false.</returns>
    public async Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await PriceHistory.AnyAsync(x => x.EntryDate == today, ct);
    }

    #region Metals

    /// <summary>Returns all metals from the <c>metals</c> table.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all metals.</returns>
    public async Task<IEnumerable<Metals>> GetMetalsAll(CancellationToken ct)
    {
        return await Metals.ToListAsync(ct);
    }

    /// <summary>Returns a single metal by its primary key.</summary>
    /// <param name="id">The metal ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The metal if found; otherwise <c>null</c>.</returns>
    public async Task<Metals?> GetMetalById(int id, CancellationToken ct)
    {
        return await Metals.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    /// <summary>Returns a single metal by its symbol (e.g. XAU, XAG, XPT).</summary>
    /// <param name="symbol">The metal symbol.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The metal if found; otherwise <c>null</c>.</returns>
    public async Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct)
    {
        return await Metals.FirstOrDefaultAsync(x => x.Symbol == symbol, ct);
    }

    #endregion Metals

    #region PriceHistory

    public async Task<PriceHistory?> GetLastPriceHistoryEntryByMetalId(int metalId, CancellationToken ct)
    {
        return await PriceHistory.Where(x => x.MetalId == metalId)
                                 .OrderByDescending(x => x.EntryDate)
                                 .FirstOrDefaultAsync(ct);
    }

    public async Task<PriceHistory?> GetLastPriceHistoryEntryByMetalSymbol(string symbol, CancellationToken ct)
    {
        return await PriceHistory.Where(x => x.Symbol == symbol)
                                 .OrderByDescending(x => x.EntryDate)
                                 .FirstOrDefaultAsync(ct);
    }

    /// <summary>Returns all rows from the <c>price_history</c> table.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all price history entries.</returns>
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAll(CancellationToken ct)
    {
        return await PriceHistory.ToListAsync(ct);
    }

    /// <summary>Returns price history for a specific metal by its ID.</summary>
    /// <param name="metalId">The metal ID (foreign key).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Price history for the given metal.</returns>
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalId(int metalId, CancellationToken ct)
    {
        return await PriceHistory.Where(x => x.MetalId == metalId).ToListAsync(ct);
    }

    /// <summary>Returns price history for a specific metal by its symbol.</summary>
    /// <param name="symbol">The metal symbol (e.g. XAU, XAG).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Price history for the given metal.</returns>
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken ct)
    {
        return await PriceHistory.Where(x => x.Symbol == symbol).ToListAsync(ct);
    }

    /// <summary>Returns all price history within a date range (inclusive).</summary>
    /// <param name="firstDate">Start date of the range.</param>
    /// <param name="lastDate">End date of the range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Price history entries where <see cref="PriceHistory.EntryDate"/> is between the two dates.</returns>
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate, CancellationToken ct)
    {
        return await PriceHistory.Where(x => x.EntryDate >= firstDate &&
                                        x.EntryDate <= lastDate).ToListAsync(ct);
    }

    /// <summary>Returns price history for a metal (by ID) within a date range (inclusive).</summary>
    /// <param name="metalId">The metal ID.</param>
    /// <param name="firstDate">Start date of the range.</param>
    /// <param name="lastDate">End date of the range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Price history for the metal in the given date range.</returns>
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalIdAndDateRange(int metalId, DateOnly firstDate, DateOnly lastDate, CancellationToken ct)
    {
        return await PriceHistory.Where(x => x.MetalId == metalId &&
                                        x.EntryDate >= firstDate &&
                                        x.EntryDate <= lastDate).ToListAsync(ct);
    }

    /// <summary>Returns price history for a metal (by symbol) within a date range (inclusive).</summary>
    /// <param name="symbol">The metal symbol (e.g. XAU, XAG).</param>
    /// <param name="firstDate">Start date of the range.</param>
    /// <param name="lastDate">End date of the range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Price history for the metal in the given date range.</returns>
    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct)
    {
        return await PriceHistory.Where(x => x.Symbol == symbol &&
                                        x.EntryDate >= firstDate &&
                                        x.EntryDate <= lastDate).ToListAsync(ct);
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
