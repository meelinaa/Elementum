using Elementum.Domain.Entities;
using Elementum.Domain.Models;
using Elementum.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Worker.Tests.Services;

public class PriceHistoryRepositoryTests
{
    private static ElementumDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ElementumDbContext(options);
        context.Metals.Add(new Metals { Id = 1, Symbol = "XAU", Name = "Gold" });
        context.Metals.Add(new Metals { Id = 2, Symbol = "XAG", Name = "Silver" });
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task SavePricesAsync_WhenPricesEmpty_DoesNotThrow()
    {
        await using var db = CreateDbContext();
        await db.SavePricesAsync(Array.Empty<DailyPrices>());
        var count = await db.PriceHistory.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SavePricesAsync_WhenPricesContainValidMetal_InsertsRow()
    {
        await using var db = CreateDbContext();
        var prices = new List<DailyPrices>
        {
            new() { Metal = "XAU", Currency = "USD", Price = 2650.50m, Symbol = "FOREXCOM:XAUUSD" }
        };

        await db.SavePricesAsync(prices);

        var count = await db.PriceHistory.CountAsync();
        Assert.Equal(1, count);
        var row = await db.PriceHistory.FirstAsync();
        Assert.Equal(1, row.MetalId);
        Assert.Equal(2650.50m, row.Price);
        Assert.Equal("USD", row.Currency);
    }

    [Fact]
    public async Task GetPriceHistoryMetalData_Daily_ReturnsBoundedChronologicalRecords()
    {
        await using var db = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (int i = 10; i >= 1; i--)
        {
            db.PriceHistory.Add(new PriceHistory
            {
                MetalId = 1,
                Currency = "USD",
                EntryDate = today.AddDays(-i),
                Price = 2000m + i,
                Symbol = "FOREXCOM:XAUUSD"
            });
        }
        await db.SaveChangesAsync();

        var result = (await db.GetPriceHistoryMetalData("XAU", "daily", 5, CancellationToken.None)).ToList();

        Assert.Equal(5, result.Count);
        Assert.True(result[0].EntryDate < result[4].EntryDate); // Chronological order
        Assert.Equal(today.AddDays(-1), result[4].EntryDate);
    }

    [Fact]
    public async Task GetPriceHistoryMetalData_Monthly_ReturnsAggregatedAverages()
    {
        await using var db = CreateDbContext();
        db.PriceHistory.AddRange(
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = new DateOnly(2026, 1, 10), Price = 2000m, Symbol = "XAU" },
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = new DateOnly(2026, 1, 20), Price = 3000m, Symbol = "XAU" },
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = new DateOnly(2026, 2, 15), Price = 4000m, Symbol = "XAU" }
        );
        await db.SaveChangesAsync();

        var result = (await db.GetPriceHistoryMetalData("XAU", "monthly", 12, CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), result[0].EntryDate);
        Assert.Equal(2500m, result[0].Price); // Average of 2000 and 3000
        Assert.Equal(new DateOnly(2026, 2, 1), result[1].EntryDate);
        Assert.Equal(4000m, result[1].Price);
    }

    [Fact]
    public async Task SavePricesAsync_WhenCalledMultipleTimes_UpdatesExistingRowIdempotently()
    {
        await using var db = CreateDbContext();
        var pricesInitial = new List<DailyPrices>
        {
            new() { Metal = "XAU", Currency = "USD", Price = 2600.00m, Symbol = "FOREXCOM:XAUUSD" }
        };

        await db.SavePricesAsync(pricesInitial);
        Assert.Equal(1, await db.PriceHistory.CountAsync());
        Assert.Equal(2600.00m, (await db.PriceHistory.FirstAsync()).Price);

        // Second run with updated price on same day
        var pricesUpdated = new List<DailyPrices>
        {
            new() { Metal = "XAU", Currency = "USD", Price = 2650.00m, Symbol = "FOREXCOM:XAUUSD" }
        };

        await db.SavePricesAsync(pricesUpdated);

        // Count must still be 1, but price updated to 2650.00
        Assert.Equal(1, await db.PriceHistory.CountAsync());
        Assert.Equal(2650.00m, (await db.PriceHistory.FirstAsync()).Price);
    }

    [Fact]
    public async Task IsDataAlreadyIngestedToday_PartialVsFullIngestion_ChecksAllCatalogMetals()
    {
        await using var db = CreateDbContext(); // Contains Metal 1 (Gold) and Metal 2 (Silver)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Initially no prices ingested
        Assert.False(await db.IsDataAlreadyIngestedToday(CancellationToken.None));

        // Ingest only Gold (1 of 2 metals)
        db.PriceHistory.Add(new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = today, Price = 2000m, Symbol = "XAU" });
        await db.SaveChangesAsync();

        // Must still return false because Silver is missing
        Assert.False(await db.IsDataAlreadyIngestedToday(CancellationToken.None));

        // Ingest Silver (2 of 2 metals)
        db.PriceHistory.Add(new PriceHistory { MetalId = 2, Currency = "USD", EntryDate = today, Price = 30m, Symbol = "XAG" });
        await db.SaveChangesAsync();

        // Now all metals are ingested -> must return true
        Assert.True(await db.IsDataAlreadyIngestedToday(CancellationToken.None));
    }

    [Fact]
    public async Task SaveEdelmetallePricesAsync_SavesHourlyTicksAndUpdatesDailyCandles()
    {
        await using var db = CreateDbContext();
        var data = new EdelmetalleApiResponse
        {
            GoldUsd = 4410.6m,
            GoldEur = 3803.8m,
            SilberUsd = 65.7m,
            SilberEur = 56.6m,
            PlatinUsd = 1773.5m,
            PlatinEur = 1529.59m,
            PalladiumUsd = 1334m,
            PalladiumEur = 1150.53m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        await db.SaveEdelmetallePricesAsync(data, CancellationToken.None);

        // Gold & Silver were seeded (metals 1 and 2), each has USD and EUR
        var historyCount = await db.PriceHistory.CountAsync();
        Assert.Equal(4, historyCount);

        var candlesCount = await db.DailyPriceSummaries.CountAsync();
        Assert.Equal(4, candlesCount);

        var goldCandleUsd = await db.DailyPriceSummaries.FirstOrDefaultAsync(s => s.MetalId == 1 && s.Currency == "USD");
        Assert.NotNull(goldCandleUsd);
        Assert.Equal(4410.6m, goldCandleUsd.OpenPrice);
        Assert.Equal(4410.6m, goldCandleUsd.HighPrice);
        Assert.Equal(4410.6m, goldCandleUsd.LowPrice);
        Assert.Equal(4410.6m, goldCandleUsd.ClosePrice);
    }

    [Fact]
    public async Task PruneHourlyDataOlderThanAsync_DeletesRecordsOlderThan7Days()
    {
        await using var db = CreateDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 10 days old record
        db.PriceHistory.Add(new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = today.AddDays(-10), Price = 2000m, Symbol = "XAU" });
        // 8 days old record
        db.PriceHistory.Add(new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = today.AddDays(-8), Price = 2100m, Symbol = "XAU" });
        // 5 days old record (within 7 days retention)
        db.PriceHistory.Add(new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = today.AddDays(-5), Price = 2200m, Symbol = "XAU" });
        // Today record
        db.PriceHistory.Add(new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = today, Price = 2300m, Symbol = "XAU" });
        await db.SaveChangesAsync();

        var threshold = DateTime.UtcNow.AddDays(-7);
        var deletedCount = await db.PruneHourlyDataOlderThanAsync(threshold, CancellationToken.None);

        Assert.Equal(2, deletedCount);
        Assert.Equal(2, await db.PriceHistory.CountAsync());
    }

    [Fact]
    public async Task AggregateDailySummaryAsync_ComputesMinMaxOpenCloseAccurately()
    {
        await using var db = CreateDbContext();
        var date = new DateOnly(2026, 8, 17);

        db.PriceHistory.AddRange(
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = date, Price = 4400m, ReferenceTimestamp = "1000", Symbol = "XAU" },
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = date, Price = 4480m, ReferenceTimestamp = "1001", Symbol = "XAU" }, // High
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = date, Price = 4390m, ReferenceTimestamp = "1002", Symbol = "XAU" }, // Low
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = date, Price = 4420m, ReferenceTimestamp = "1003", Symbol = "XAU" }  // Close
        );
        await db.SaveChangesAsync();

        await db.AggregateDailySummaryAsync(date, CancellationToken.None);

        var candle = await db.DailyPriceSummaries.FirstOrDefaultAsync(s => s.MetalId == 1 && s.Currency == "USD" && s.EntryDate == date);
        Assert.NotNull(candle);
        Assert.Equal(4400m, candle.OpenPrice);
        Assert.Equal(4480m, candle.HighPrice);
        Assert.Equal(4390m, candle.LowPrice);
        Assert.Equal(4420m, candle.ClosePrice);
    }
}
