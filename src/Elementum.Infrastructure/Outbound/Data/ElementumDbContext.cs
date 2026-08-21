using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Infrastructure.Outbound.Data;

/// <summary>
/// Entity Framework Core DbContext for Elementum.
/// Provides pure relational schema mapping and DbSets for MySQL database tables.
/// </summary>
public class ElementumDbContext(DbContextOptions<ElementumDbContext> options) : DbContext(options)
{
    /// <summary>DbSet for the <c>metals</c> table.</summary>
    public DbSet<Metals> Metals => Set<Metals>();

    /// <summary>DbSet for the <c>price_history</c> table.</summary>
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();

    /// <summary>DbSet for the <c>distributed_locks</c> table.</summary>
    public DbSet<DistributedLockEntity> DistributedLocks => Set<DistributedLockEntity>();

    /// <summary>DbSet for the <c>daily_price_summaries</c> table (consolidated daily candles: 22:00 Close, Min, Max, Open).</summary>
    public DbSet<DailyPriceSummary> DailyPriceSummaries => Set<DailyPriceSummary>();

    /// <summary>
    /// Configures global decimal precision (18, 4) for all currency and spot price properties across the entire domain model.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }

    /// <summary>Configures the entity model: table names, keys, precision, and column mappings.</summary>
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
            e.HasIndex(x => new { x.MetalId, x.Currency, x.ReferenceTimestamp }).IsUnique();
            e.HasIndex(x => new { x.MetalId, x.Currency, x.EntryDate });
            e.HasOne(x => x.Metal).WithMany(m => m.PriceHistory).HasForeignKey(x => x.MetalId).OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.MetalId).HasColumnName("metal_id");
            e.Property(x => x.ReferenceTimestamp).HasColumnName("reference_timestamp");
            e.Property(x => x.EntryDate).HasColumnName("entry_date");
            e.Property(x => x.Price).HasColumnName("price");
            e.Property(x => x.PrevClosePrice).HasColumnName("prev_close_price");
            e.Property(x => x.OpenPrice).HasColumnName("open_price");
            e.Property(x => x.LowPrice).HasColumnName("low_price");
            e.Property(x => x.HighPrice).HasColumnName("high_price");
            e.Property(x => x.Ch).HasColumnName("ch");
            e.Property(x => x.Chp).HasColumnName("chp");
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
            e.Property(x => x.OpenPrice).HasColumnName("open_price").IsRequired();
            e.Property(x => x.HighPrice).HasColumnName("high_price").IsRequired();
            e.Property(x => x.LowPrice).HasColumnName("low_price").IsRequired();
            e.Property(x => x.ClosePrice).HasColumnName("close_price").IsRequired();
            e.Property(x => x.ExchangeRateUsdEur).HasColumnName("exchange_rate_usd_eur").HasPrecision(18, 8);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsConcurrencyToken().IsRequired();
        });
    }
}
