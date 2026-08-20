using Elementum.Domain.Entities;
using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Elementum.UnitTests.Infrastructure.Data;

public class OptimisticConcurrencyTests
{
    private static (ElementumDbContext, InMemoryDatabaseRoot, string) CreateDb()
    {
        var root = new InMemoryDatabaseRoot();
        var name = "ConcurrencyTestDb_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(name, root)
            .Options;
        var db = new ElementumDbContext(options);
        db.Database.EnsureCreated();
        return (db, root, name);
    }

    // [E]RROR / CONCURRENCY: Verifies that stale updates on DailyPriceSummary trigger DbUpdateConcurrencyException
    [Fact]
    public async Task DailyPriceSummary_WhenConcurrentModificationsOccur_HasConcurrencyTokenConfigured()
    {
        // Arrange
        var (db, root, name) = CreateDb();
        var date = new DateOnly(2026, 8, 17);

        var summary = DailyPriceSummary.Create(1, "USD", date, 2500m, 2550m, 2480m, 2520m);
        db.DailyPriceSummaries.Add(summary);
        await db.SaveChangesAsync();

        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(name, root)
            .Options;
        using var db2 = new ElementumDbContext(options);
        var summaryFromDb2 = await db2.DailyPriceSummaries.FirstAsync(s => s.Id == summary.Id);

        // Act - Instance 1 updates the entity
        summary.ApplyPriceTick(2600m);
        await db.SaveChangesAsync();

        // Instance 2 attempts to update with stale concurrency token (UpdatedAtUtc)
        summaryFromDb2.ApplyPriceTick(2400m);

        // Assert - InMemory EF Core enforces Fluent API concurrency tokens
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await db2.SaveChangesAsync();
        });
    }

    // [R]IGHT-BICEP: DailyCandleAggregator aggregates ticks into OHLC candles on the happy path
    [Fact]
    public async Task AggregateDailySummaryAsync_WhenTicksExist_ComputesOhlcWithoutErrors()
    {
        // Arrange
        var (db, _, _) = CreateDb();
        var aggregator = new DailyCandleAggregator();
        var date = new DateOnly(2026, 8, 17);

        db.Metals.Add(new Metals { Id = 1, Symbol = "XAU", Name = "Gold" });
        db.PriceHistory.AddRange(
            PriceHistory.Create(1, "USD", date, 2500m, "XAU", referenceTimestamp: 1000L),
            PriceHistory.Create(1, "USD", date, 2550m, "XAU", referenceTimestamp: 1001L)
        );
        await db.SaveChangesAsync();

        // Act - Aggregate summaries without errors
        await aggregator.AggregateDailySummaryAsync(db, date, CancellationToken.None);

        // Assert
        var candle = await db.DailyPriceSummaries.FirstOrDefaultAsync(s => s.MetalId == 1 && s.Currency == "USD" && s.EntryDate == date);
        Assert.NotNull(candle);
        Assert.Equal(2500m, candle.OpenPrice);
        Assert.Equal(2550m, candle.HighPrice);
    }
}
