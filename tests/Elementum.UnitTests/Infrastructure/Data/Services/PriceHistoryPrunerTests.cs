using Elementum.Domain.Entities;
using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Elementum.UnitTests.Infrastructure.Data.Services;

public class PriceHistoryPrunerTests
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

    // [R]IGHT-BICEP: Verifies that records strictly older than the cutoff threshold are deleted and count is returned
    [Fact]
    public async Task PruneHourlyDataOlderThanAsync_DeletesStaleRecords()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var cutoffDate = DateOnly.FromDateTime(cutoff);

        db.PriceHistory.AddRange(
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = cutoffDate.AddDays(-3), Price = 2000m, Symbol = "XAU" },
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = cutoffDate.AddDays(-1), Price = 2050m, Symbol = "XAU" },
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = cutoffDate, Price = 2100m, Symbol = "XAU" },
            new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = cutoffDate.AddDays(1), Price = 2150m, Symbol = "XAU" }
        );
        await db.SaveChangesAsync();

        var pruner = new PriceHistoryPruner();

        // Act
        var deletedCount = await pruner.PruneHourlyDataOlderThanAsync(db, cutoff, CancellationToken.None);

        // Assert
        Assert.Equal(2, deletedCount);
        Assert.Equal(2, await db.PriceHistory.CountAsync());
    }

    // [B]OUNDARY: Verifies that when no records precede the cutoff date, 0 is returned and table is untouched
    [Fact]
    public async Task PruneHourlyDataOlderThanAsync_WhenNoStaleRecords_ReturnsZero()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var cutoffDate = DateOnly.FromDateTime(cutoff);

        db.PriceHistory.Add(new PriceHistory { MetalId = 1, Currency = "USD", EntryDate = cutoffDate.AddDays(1), Price = 2500m, Symbol = "XAU" });
        await db.SaveChangesAsync();

        var pruner = new PriceHistoryPruner();

        // Act
        var deletedCount = await pruner.PruneHourlyDataOlderThanAsync(db, cutoff, CancellationToken.None);

        // Assert
        Assert.Equal(0, deletedCount);
        Assert.Equal(1, await db.PriceHistory.CountAsync());
    }
}
