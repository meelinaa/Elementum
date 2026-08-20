using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data;
using Elementum.Infrastructure.Outbound.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Elementum.UnitTests.Infrastructure.Data.Services;

public class DailyCandleAggregatorTests
{
    private static ElementumDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new ElementumDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    // [R]IGHT-BICEP: Verifies that AggregateDailySummaryAsync groups ticks by metal & currency and computes correct OHLC candles
    [Fact]
    public async Task AggregateDailySummaryAsync_ComputesCorrectOhlcCandles()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var date = new DateOnly(2026, 8, 17);

        db.PriceHistory.AddRange(
            PriceHistory.Create(1, "USD", date, 2400m, "XAU", referenceTimestamp: 100),
            PriceHistory.Create(1, "USD", date, 2450m, "XAU", referenceTimestamp: 200),
            PriceHistory.Create(1, "USD", date, 2390m, "XAU", referenceTimestamp: 300),
            PriceHistory.Create(1, "USD", date, 2420m, "XAU", referenceTimestamp: 400)
        );
        await db.SaveChangesAsync();

        var aggregator = new DailyCandleAggregator();

        // Act
        await aggregator.AggregateDailySummaryAsync(db, date, CancellationToken.None);

        // Assert
        var summary = await db.DailyPriceSummaries.FirstOrDefaultAsync(s => s.MetalId == 1 && s.Currency == "USD" && s.EntryDate == date);
        Assert.NotNull(summary);
        Assert.Equal(2400m, summary.OpenPrice);
        Assert.Equal(2450m, summary.HighPrice);
        Assert.Equal(2390m, summary.LowPrice);
        Assert.Equal(2420m, summary.ClosePrice);
    }

    // [B]OUNDARY: Verifies that when no ticks exist for the given date, execution exits without errors or modifications
    [Fact]
    public async Task AggregateDailySummaryAsync_WhenNoTicks_ExitsCleanly()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var date = new DateOnly(2026, 8, 17);
        var aggregator = new DailyCandleAggregator();

        // Act
        await aggregator.AggregateDailySummaryAsync(db, date, CancellationToken.None);

        // Assert
        Assert.Equal(0, await db.DailyPriceSummaries.CountAsync());
    }

    // [I]NVERSE / IDEMPOTENCY: Verifies that repeated aggregate calls update existing summaries without duplicate rows
    [Fact]
    public async Task AggregateDailySummaryAsync_WhenSummaryExists_UpdatesInPlaceWithoutDuplicates()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var date = new DateOnly(2026, 8, 17);

        db.PriceHistory.Add(PriceHistory.Create(1, "USD", date, 2400m, "XAU", referenceTimestamp: 100));
        await db.SaveChangesAsync();

        var aggregator = new DailyCandleAggregator();
        await aggregator.AggregateDailySummaryAsync(db, date, CancellationToken.None);

        // Add a new higher tick later in the day
        db.PriceHistory.Add(PriceHistory.Create(1, "USD", date, 2600m, "XAU", referenceTimestamp: 200));
        await db.SaveChangesAsync();

        // Act - 2nd run
        await aggregator.AggregateDailySummaryAsync(db, date, CancellationToken.None);

        // Assert
        var count = await db.DailyPriceSummaries.CountAsync();
        Assert.Equal(1, count);

        var updated = await db.DailyPriceSummaries.FirstAsync();
        Assert.Equal(2600m, updated.HighPrice);
        Assert.Equal(2600m, updated.ClosePrice);
    }

    // [R]IGHT-BICEP: Verifies that UpdateSummaryForTickAsync initializes new summary on first tick of the day
    [Fact]
    public async Task UpdateSummaryForTickAsync_WhenNoExistingSummary_CreatesNewDailySummary()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var date = new DateOnly(2026, 8, 17);
        var aggregator = new DailyCandleAggregator();

        // Act
        await aggregator.UpdateSummaryForTickAsync(db, 1, "USD", date, 2500m, 1.15m, isCloseHour: false, CancellationToken.None);
        await db.SaveChangesAsync();

        // Assert
        var summary = await db.DailyPriceSummaries.FirstOrDefaultAsync(s => s.MetalId == 1 && s.Currency == "USD" && s.EntryDate == date);
        Assert.NotNull(summary);
        Assert.Equal(2500m, summary.OpenPrice);
        Assert.Equal(2500m, summary.HighPrice);
        Assert.Equal(2500m, summary.LowPrice);
        Assert.Equal(2500m, summary.ClosePrice);
    }

    // [R]IGHT-BICEP: Verifies that UpdateSummaryForTickAsync updates existing summary on subsequent tick
    [Fact]
    public async Task UpdateSummaryForTickAsync_WhenSummaryExists_UpdatesHighAndClosePrice()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var date = new DateOnly(2026, 8, 17);
        var aggregator = new DailyCandleAggregator();

        await aggregator.UpdateSummaryForTickAsync(db, 1, "USD", date, 2500m, 1.15m, isCloseHour: false, CancellationToken.None);
        await db.SaveChangesAsync();

        // Act - Subsequent tick with higher price and closing hour
        await aggregator.UpdateSummaryForTickAsync(db, 1, "USD", date, 2550m, 1.15m, isCloseHour: true, CancellationToken.None);
        await db.SaveChangesAsync();

        // Assert
        var summary = await db.DailyPriceSummaries.FirstAsync(s => s.MetalId == 1 && s.Currency == "USD" && s.EntryDate == date);
        Assert.Equal(2500m, summary.OpenPrice);
        Assert.Equal(2550m, summary.HighPrice);
        Assert.Equal(2550m, summary.ClosePrice);
    }

    // [E]RROR RIGHT-BICEP: DbUpdateConcurrencyException during save is reconciled via reload without throwing
    [Fact]
    public async Task AggregateDailySummaryAsync_WhenConcurrencyConflictOccurs_ReconcilesViaReload()
    {
        // Arrange
        var root = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
        var databaseName = "AggregatorConcurrency_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .Options;

        await using var db1 = new ElementumDbContext(options);
        db1.Database.EnsureCreated();

        var date = new DateOnly(2026, 8, 17);
        db1.PriceHistory.Add(PriceHistory.Create(1, "USD", date, 2400m, "XAU", referenceTimestamp: 100));
        await db1.SaveChangesAsync();

        var aggregator = new DailyCandleAggregator();
        await aggregator.AggregateDailySummaryAsync(db1, date, CancellationToken.None);

        await using var db2 = new ElementumDbContext(options);
        var summaryOnDb2 = await db2.DailyPriceSummaries.SingleAsync();
        summaryOnDb2.ApplyPriceTick(2410m, 1.1m, isClosePrice: false);
        await db2.SaveChangesAsync();

        db1.PriceHistory.Add(PriceHistory.Create(1, "USD", date, 2600m, "XAU", referenceTimestamp: 200));
        await db1.SaveChangesAsync();

        // Act
        var thrown = await Record.ExceptionAsync(() => aggregator.AggregateDailySummaryAsync(db1, date, CancellationToken.None));

        // Assert - concurrency reload path must not crash the aggregation step
        Assert.Null(thrown);
        Assert.Equal(1, await db1.DailyPriceSummaries.CountAsync());
    }
}
