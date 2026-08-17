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
}
