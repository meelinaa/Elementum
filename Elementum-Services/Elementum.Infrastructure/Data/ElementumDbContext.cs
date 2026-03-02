using Elementum.Shared.Objects;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for Elementum. Maps to existing MySQL schema: metals, price_history.
/// </summary>
public class ElementumDbContext : DbContext
{
    public ElementumDbContext(DbContextOptions<ElementumDbContext> options)
        : base(options)
    {
    }

    public DbSet<Metals> Metals => Set<Metals>();
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();

    /// <summary>
    /// Returns whether any row in <c>price_history</c> has <see cref="PriceHistory.EntryDate"/> equal to today (UTC).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if at least one such row exists; otherwise false.</returns>
    public async Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await PriceHistory.AnyAsync(x => x.EntryDate == today, ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // metals (lowercase table name)
        modelBuilder.Entity<Metals>(e =>
        {
            e.ToTable("metals");
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        // price_history (snake_case columns)
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
}
