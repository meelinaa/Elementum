using Elementum.Infrastructure.Data.Interfaces;
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

    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct)
    {
        return await PriceHistory
            .Include(x => x.Metal)
            .GroupBy(x => x.MetalId)
            .Select(group => group
                .OrderByDescending(x => x.EntryDate)
                .First())
            .ToListAsync(ct);
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
