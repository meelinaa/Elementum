using Elementum.Application.Mapping;
using Elementum.Application.Models;
using Elementum.Domain.Entities;
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

    // [I]NVERSE / IDEMPOTENCY: Verifies that saving the identical timestamped tick payload multiple times is strictly idempotent
    [Fact]
    public async Task SavePricesAsync_WhenRunTwiceWithSameTimestamp_DoesNotCreateDuplicateTicks()
    {
        // Arrange
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

        var metalsMap = db.Metals.ToDictionary(m => m.Symbol, m => m.Id);
        var entities = payload.ToPriceHistoryEntities(metalsMap, DateOnly.FromDateTime(DateTime.UtcNow));

        // Act - 1st execution
        await repository.SavePricesAsync(entities);
        var countAfterFirstRun = await db.PriceHistory.CountAsync();

        // Act - 2nd execution with exact same timestamp (e.g. worker retry or recovery)
        await repository.SavePricesAsync(entities);
        var countAfterSecondRun = await db.PriceHistory.CountAsync();

        // Assert
        Assert.Equal(8, countAfterFirstRun); // 4 metals * 2 currencies (USD & EUR)
        Assert.Equal(8, countAfterSecondRun); // Must remain strictly 8 with no duplicates
    }
}
