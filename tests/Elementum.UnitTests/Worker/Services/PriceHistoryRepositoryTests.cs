using Elementum.Application.Mapping;
using Elementum.Application.Models;
using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data;
using Elementum.Infrastructure.Outbound.Data.Repositories;
using Elementum.Infrastructure.Outbound.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Worker.Tests.Services;

public class PriceHistoryRepositoryTests
{
    private static (ElementumDbContext Db, PriceHistoryRepository Repo) CreateTestSetup()
    {
        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ElementumDbContext(options);
        context.Metals.Add(new Metals { Id = 1, Symbol = "XAU", Name = "Gold" });
        context.Metals.Add(new Metals { Id = 2, Symbol = "XAG", Name = "Silver" });
        context.SaveChanges();

        var aggregator = new DailyCandleAggregator();
        var pruner = new PriceHistoryPruner();
        var repo = new PriceHistoryRepository(context, aggregator, pruner);

        return (context, repo);
    }

    private static PriceHistory Tick(
        int metalId,
        DateOnly date,
        decimal price,
        string currency = "USD",
        string symbol = "XAU",
        long timestamp = 0) =>
        PriceHistory.Create(metalId, currency, date, price, symbol, referenceTimestamp: timestamp);

    // RIGHT-[B]ICEP: Verifies that saving an empty list of prices handles the boundary without exceptions or mutations
    [Fact]
    public async Task SavePricesAsync_WhenPricesEmpty_DoesNotThrow()
    {
        // Arrange
        var (db, repo) = CreateTestSetup();

        // Act
        await repo.SavePricesAsync(Array.Empty<PriceHistory>());
        var count = await db.PriceHistory.CountAsync();

        // Assert
        Assert.Equal(0, count);
    }

    // [R]IGHT-BICEP: Verifies that saving valid metal prices inserts a new row with accurate fields
    [Fact]
    public async Task SavePricesAsync_WhenPricesContainValidMetal_InsertsRow()
    {
        // Arrange
        var (db, repo) = CreateTestSetup();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prices = new List<PriceHistory>
        {
            PriceHistory.Create(metalId: 1, currency: "USD", entryDate: today, price: 2650.50m, symbol: "XAUUSD", referenceTimestamp: 100L)
        };

        // Act
        await repo.SavePricesAsync(prices);

        // Assert
        var count = await db.PriceHistory.CountAsync();
        Assert.Equal(1, count);
        var row = await db.PriceHistory.FirstAsync();
        Assert.Equal(1, row.MetalId);
        Assert.Equal(2650.50m, row.Price);
        Assert.Equal("USD", row.Currency);
    }

    // RIGHT-B[I]CEP: Verifies that repeated saves with identical timestamp update existing records idempotently
    [Fact]
    public async Task SavePricesAsync_WhenCalledMultipleTimes_UpdatesExistingRowIdempotently()
    {
        // Arrange
        var (db, repo) = CreateTestSetup();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var initialPrices = new List<PriceHistory>
        {
            PriceHistory.Create(metalId: 1, currency: "USD", entryDate: today, price: 2600.00m, symbol: "XAUUSD", referenceTimestamp: 1000L)
        };

        // Act - Initial save
        await repo.SavePricesAsync(initialPrices);
        Assert.Equal(1, await db.PriceHistory.CountAsync());
        Assert.Equal(2600.00m, (await db.PriceHistory.FirstAsync()).Price);

        // Act - Second save with updated quote on the same timestamp
        var updatedPrices = new List<PriceHistory>
        {
            PriceHistory.Create(metalId: 1, currency: "USD", entryDate: today, price: 2650.00m, symbol: "XAUUSD", referenceTimestamp: 1000L)
        };
        await repo.SavePricesAsync(updatedPrices);

        // Assert - Count must remain strictly 1, price updated to 2650.00
        Assert.Equal(1, await db.PriceHistory.CountAsync());
        Assert.Equal(2650.00m, (await db.PriceHistory.FirstAsync()).Price);
    }

    // RIGHT-[B]ICEP: Verifies partial versus full metal catalog ingestion boundaries for today's check
    [Fact]
    public async Task IsDataAlreadyIngestedToday_PartialVsFullIngestion_ChecksAllCatalogMetals()
    {
        // Arrange
        var (db, repo) = CreateTestSetup(); // Contains Metal 1 (Gold) and Metal 2 (Silver)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act & Assert - Initially no prices ingested
        Assert.False(await repo.IsDataAlreadyIngestedToday(CancellationToken.None));

        // Act & Assert - Ingest only Gold (1 of 2 metals)
        db.PriceHistory.Add(Tick(1, today, 2000m));
        await db.SaveChangesAsync();
        Assert.False(await repo.IsDataAlreadyIngestedToday(CancellationToken.None));

        // Act & Assert - Ingest Silver (2 of 2 metals)
        db.PriceHistory.Add(Tick(2, today, 30m, symbol: "XAG"));
        await db.SaveChangesAsync();
        Assert.True(await repo.IsDataAlreadyIngestedToday(CancellationToken.None));
    }

    // [R]IGHT-BICEP: Verifies that saving live tick responses creates price history ticks and updates daily candles
    [Fact]
    public async Task SavePricesAsync_SavesHourlyTicksAndUpdatesDailyCandles()
    {
        // Arrange
        var (db, repo) = CreateTestSetup();
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

        var metalsMap = db.Metals.ToDictionary(m => m.Symbol, m => m.Id);
        var entities = data.ToPriceHistoryEntities(metalsMap, DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        await repo.SavePricesAsync(entities, CancellationToken.None);

        // Assert - Gold & Silver were seeded (metals 1 and 2), each has USD and EUR
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

    // [R]IGHT-BICEP: Verifies that retention pruning removes hourly ticks older than the threshold
    [Fact]
    public async Task PruneHourlyDataOlderThanAsync_DeletesRecordsOlderThan7Days()
    {
        // Arrange
        var (db, repo) = CreateTestSetup();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        db.PriceHistory.Add(Tick(1, today.AddDays(-10), 2000m));
        db.PriceHistory.Add(Tick(1, today.AddDays(-8), 2100m));
        db.PriceHistory.Add(Tick(1, today.AddDays(-5), 2200m));
        db.PriceHistory.Add(Tick(1, today, 2300m));
        await db.SaveChangesAsync();

        // Act
        var threshold = DateTime.UtcNow.AddDays(-7);
        var deletedCount = await repo.PruneHourlyDataOlderThanAsync(threshold, CancellationToken.None);

        // Assert
        Assert.Equal(2, deletedCount);
        Assert.Equal(2, await db.PriceHistory.CountAsync());
    }

    // RIGHT-BI[C]EP: Cross-checks aggregate daily summary output against independently calculated tick extrema
    [Fact]
    public async Task AggregateDailySummaryAsync_ComputesMinMaxOpenCloseAccurately()
    {
        // Arrange
        var (db, repo) = CreateTestSetup();
        var date = new DateOnly(2026, 8, 17);

        db.PriceHistory.AddRange(
            Tick(1, date, 4400m, timestamp: 1000L),
            Tick(1, date, 4480m, timestamp: 1001L),
            Tick(1, date, 4390m, timestamp: 1002L),
            Tick(1, date, 4420m, timestamp: 1003L)
        );
        await db.SaveChangesAsync();

        // Act
        await repo.AggregateDailySummaryAsync(date, CancellationToken.None);

        // Assert
        var candle = await db.DailyPriceSummaries.FirstOrDefaultAsync(s => s.MetalId == 1 && s.Currency == "USD" && s.EntryDate == date);
        Assert.NotNull(candle);
        Assert.Equal(4400m, candle.OpenPrice);
        Assert.Equal(4480m, candle.HighPrice);
        Assert.Equal(4390m, candle.LowPrice);
        Assert.Equal(4420m, candle.ClosePrice);
    }

    // [R]IGHT-BICEP: latest tick is MAX(ReferenceTimestamp) per metal, not the highest auto-increment Id
    [Fact]
    public async Task GetPriceHistoryAllLatest_WhenNewerIdHasOlderTimestamp_ReturnsMaxReferenceTimestamp()
    {
        var (db, repo) = CreateTestSetup();
        var date = new DateOnly(2026, 8, 21);
        db.PriceHistory.Add(Tick(1, date, 2500m, timestamp: 200L));
        db.PriceHistory.Add(Tick(2, date, 30m, currency: "USD", symbol: "XAG", timestamp: 50L));
        await db.SaveChangesAsync();
        db.PriceHistory.Add(Tick(1, date, 2400m, timestamp: 100L));
        db.PriceHistory.Add(Tick(2, date, 32m, currency: "USD", symbol: "XAG", timestamp: 80L));
        await db.SaveChangesAsync();

        var latest = (await repo.GetPriceHistoryAllLatest(CancellationToken.None)).ToList();

        Assert.Equal(2, latest.Count);
        Assert.Equal(2500m, Assert.Single(latest, r => r.MetalId == 1).Price);
        Assert.Equal(200L, latest.Single(r => r.MetalId == 1).ReferenceTimestamp);
        Assert.Equal(32m, Assert.Single(latest, r => r.MetalId == 2).Price);
        Assert.Equal(80L, latest.Single(r => r.MetalId == 2).ReferenceTimestamp);
    }

    // [R]IGHT-BICEP: currency filter is applied in the repository, not via IQueryable composition in Application
    [Fact]
    public async Task GetPriceHistoryByMetalSymbolAsync_WhenCurrencyProvided_ReturnsOnlyMatchingCurrency()
    {
        // Arrange
        var (db, repo) = CreateTestSetup();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.PriceHistory.AddRange(
            Tick(1, today, 2500m, timestamp: 1000L),
            Tick(1, today, 2300m, currency: "EUR", timestamp: 1001L)
        );
        await db.SaveChangesAsync();

        // Act
        var usd = await repo.GetPriceHistoryByMetalSymbolAsync("XAU", "eur", CancellationToken.None);
        var all = await repo.GetPriceHistoryByMetalSymbolAsync("XAU", currency: null, CancellationToken.None);

        // Assert
        Assert.Single(usd);
        Assert.Equal("EUR", usd[0].Currency);
        Assert.Equal(2300m, usd[0].Price);
        Assert.Equal(2, all.Count);
    }

    // RIGHT-[B]ICEP: date-range query is inclusive and respects currency
    [Fact]
    public async Task GetPriceHistoryByMetalSymbolAndDateRangeAsync_FiltersByInclusiveRangeAndCurrency()
    {
        // Arrange
        var (db, repo) = CreateTestSetup();
        db.PriceHistory.AddRange(
            Tick(1, new DateOnly(2026, 8, 1), 2400m, timestamp: 1L),
            Tick(1, new DateOnly(2026, 8, 10), 2500m, timestamp: 2L),
            Tick(1, new DateOnly(2026, 8, 10), 2300m, currency: "EUR", timestamp: 3L),
            Tick(1, new DateOnly(2026, 8, 20), 2600m, timestamp: 4L)
        );
        await db.SaveChangesAsync();

        // Act
        var result = await repo.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
            "XAU", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 10), "USD", skip: 0, take: 100, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, row => Assert.Equal("USD", row.Currency));
        Assert.Equal(2400m, result.Items[0].Price);
        Assert.Equal(2500m, result.Items[1].Price);
    }

    // RIGHT-[B]ICEP: skip/take page the filtered set and report the unpaged total
    [Fact]
    public async Task GetPriceHistoryByMetalSymbolAndDateRangeAsync_WhenSkipAndTake_ReturnsPageAndTotalCount()
    {
        var (db, repo) = CreateTestSetup();
        db.PriceHistory.AddRange(
            Tick(1, new DateOnly(2026, 8, 1), 2400m, timestamp: 1L),
            Tick(1, new DateOnly(2026, 8, 2), 2500m, timestamp: 2L),
            Tick(1, new DateOnly(2026, 8, 3), 2600m, timestamp: 3L)
        );
        await db.SaveChangesAsync();

        var result = await repo.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
            "XAU", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), "USD", skip: 1, take: 1, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        var row = Assert.Single(result.Items);
        Assert.Equal(2500m, row.Price);
    }

    // [R]IGHT-BICEP: metals catalog is returned as a materialized list
    [Fact]
    public async Task GetMetalsAsync_ReturnsCatalog()
    {
        // Arrange
        var (_, repo) = CreateTestSetup();

        // Act
        var metals = await repo.GetMetalsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, metals.Count);
        Assert.Contains(metals, m => m.Symbol == "XAU");
        Assert.Contains(metals, m => m.Symbol == "XAG");
    }

    // [R]IGHT-BICEP: live session query returns all metals/currencies in the inclusive date window
    [Fact]
    public async Task GetDailySummariesAsync_ByDateRange_ReturnsInclusiveWindow()
    {
        // Arrange
        var (db, repo) = CreateTestSetup();
        var gold = await db.Metals.FirstAsync(m => m.Symbol == "XAU");
        var inRange = new DateOnly(2026, 8, 19);
        var alsoInRange = new DateOnly(2026, 8, 20);
        var outOfRange = new DateOnly(2026, 8, 18);

        db.DailyPriceSummaries.Add(DailyPriceSummary.Create(gold.Id, "USD", outOfRange, 2300m, 2310m, 2290m, 2305m));
        db.DailyPriceSummaries.Add(DailyPriceSummary.Create(gold.Id, "USD", inRange, 2400m, 2420m, 2390m, 2410m));
        db.DailyPriceSummaries.Add(DailyPriceSummary.Create(gold.Id, "EUR", alsoInRange, 2100m, 2120m, 2090m, 2110m));
        await db.SaveChangesAsync();

        // Act
        var result = await repo.GetDailySummariesAsync(inRange, alsoInRange, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, s => s.EntryDate == outOfRange);
        Assert.Contains(result, s => s.Currency == "USD" && s.OpenPrice == 2400m);
        Assert.Contains(result, s => s.Currency == "EUR" && s.OpenPrice == 2100m);
        Assert.All(result, s => Assert.Equal("XAU", s.Metal?.Symbol));
    }

    // [R]IGHT-BICEP: SavePricesAsync uses configured DailyRollupHour to decide isCloseHour
    [Fact]
    public async Task SavePricesAsync_WhenDailyRollupHourConfigured_UpdatesCandleClosePrice()
    {
        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ElementumDbContext(options);
        context.Metals.Add(new Metals { Id = 1, Symbol = "XAU", Name = "Gold" });
        await context.SaveChangesAsync();

        var scheduleOptions = Microsoft.Extensions.Options.Options.Create(new Elementum.Application.Options.WorkerScheduleOptions
        {
            DailyRollupHour = 0,
            IngestionIntervalMinutes = 60,
            RetentionDays = 7
        });

        var aggregator = new DailyCandleAggregator();
        var pruner = new PriceHistoryPruner();
        var repo = new PriceHistoryRepository(context, aggregator, pruner, scheduleOptions);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var initialCandle = DailyPriceSummary.Create(1, "USD", today, 2500m, 2550m, 2480m, 2510m);
        context.DailyPriceSummaries.Add(initialCandle);
        await context.SaveChangesAsync();

        var newTick = new List<PriceHistory>
        {
            PriceHistory.Create(1, "USD", today, 2530m, "XAUUSD", referenceTimestamp: 5000L)
        };

        await repo.SavePricesAsync(newTick, CancellationToken.None);

        var updatedCandle = await context.DailyPriceSummaries.FirstAsync(s => s.MetalId == 1 && s.Currency == "USD");
        Assert.Equal(2530m, updatedCandle.ClosePrice);
    }
}
