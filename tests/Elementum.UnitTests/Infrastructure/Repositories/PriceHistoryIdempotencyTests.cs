using Elementum.Domain.Entities;
using Elementum.Domain.Models;
using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.Data.Repositories;
using Elementum.Infrastructure.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Elementum.UnitTests.Infrastructure.Repositories;

public class PriceHistoryIdempotencyTests
{
    private static ElementumDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var db = new ElementumDbContext(options);
        db.Database.EnsureCreated();

        if (!db.Metals.Any())
        {
            db.Metals.AddRange(
                new Metals { Id = 1, Symbol = "XAU", Name = "Gold" },
                new Metals { Id = 2, Symbol = "XAG", Name = "Silver" },
                new Metals { Id = 3, Symbol = "XPT", Name = "Platinum" },
                new Metals { Id = 4, Symbol = "XPD", Name = "Palladium" });
            db.SaveChanges();
        }

        return db;
    }

    [Fact]
    public async Task SaveEdelmetallePricesAsync_WhenRunTwiceWithSameTimestamp_DoesNotCreateDuplicateTicks()
    {
        var db = CreateInMemoryDbContext("IdempotencyTest_" + Guid.NewGuid());
        var aggregator = new DailyCandleAggregator();
        var pruner = new PriceHistoryPruner();
        var repository = new PriceHistoryRepository(db, aggregator, pruner);

        var payload = new EdelmetalleApiResponse
        {
            GoldUsd = 2500.00m,
            GoldEur = 2280.00m,
            SilberUsd = 30.00m,
            SilberEur = 27.00m,
            PlatinUsd = 1000.00m,
            PlatinEur = 910.00m,
            PalladiumUsd = 1050.00m,
            PalladiumEur = 960.00m,
            WechselkursUsdEur = 1.095m,
            Timestamp = 1723900000
        };

        // 1st execution
        await repository.SaveEdelmetallePricesAsync(payload);

        var countAfterFirstRun = await db.PriceHistory.CountAsync();
        Assert.Equal(8, countAfterFirstRun); // 4 metals * 2 currencies (USD & EUR)

        // 2nd execution with exact same timestamp (e.g. worker retry or crash recovery)
        await repository.SaveEdelmetallePricesAsync(payload);

        var countAfterSecondRun = await db.PriceHistory.CountAsync();
        Assert.Equal(8, countAfterSecondRun); // Must still be 8, no duplicates!
    }
}
