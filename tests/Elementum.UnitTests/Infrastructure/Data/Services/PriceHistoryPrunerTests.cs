using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data;
using Elementum.Infrastructure.Outbound.Data.Services;
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

    // [B]OUNDARY RIGHT-BICEP: EntryDate equal to cutoff is retained; only strictly older rows are deleted (off-by-one)
    [Fact]
    public async Task PruneHourlyDataOlderThanAsync_WhenEntryDateEqualsCutoff_RetainsRecordAndDeletesOnlyOlder()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var cutoffDate = DateOnly.FromDateTime(cutoff);

        db.PriceHistory.AddRange(
            PriceHistory.Create(1, "USD", cutoffDate.AddDays(-3), 2000m, "XAU"),
            PriceHistory.Create(1, "USD", cutoffDate.AddDays(-1), 2050m, "XAU"),
            PriceHistory.Create(1, "USD", cutoffDate, 2100m, "XAU"),
            PriceHistory.Create(1, "USD", cutoffDate.AddDays(1), 2150m, "XAU")
        );
        await db.SaveChangesAsync();

        var pruner = new PriceHistoryPruner();

        // Act
        var deletedCount = await pruner.PruneHourlyDataOlderThanAsync(db, cutoff, CancellationToken.None);

        // Assert
        Assert.Equal(2, deletedCount);
        Assert.Equal(2, await db.PriceHistory.CountAsync());
    }

    // [B]OUNDARY RIGHT-BICEP: when all records are on or after cutoff date, zero deletions and unchanged row count
    [Fact]
    public async Task PruneHourlyDataOlderThanAsync_WhenNoStaleRecords_ReturnsZero()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var cutoffDate = DateOnly.FromDateTime(cutoff);

        db.PriceHistory.Add(PriceHistory.Create(1, "USD", cutoffDate.AddDays(1), 2500m, "XAU"));
        await db.SaveChangesAsync();

        var pruner = new PriceHistoryPruner();

        // Act
        var deletedCount = await pruner.PruneHourlyDataOlderThanAsync(db, cutoff, CancellationToken.None);

        // Assert
        Assert.Equal(0, deletedCount);
        Assert.Equal(1, await db.PriceHistory.CountAsync());
    }

    // [I]NVERSE RIGHT-BICEP: after partial manual deletion, re-run pruner removes exactly the remaining stale rows
    [Fact]
    public async Task PruneHourlyDataOlderThanAsync_WhenPartiallyDeleted_ReRunRemovesRemainingStaleRows()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var cutoffDate = DateOnly.FromDateTime(cutoff);

        var staleRecords = Enumerable.Range(0, 5)
            .Select(i => PriceHistory.Create(1, "USD", cutoffDate.AddDays(-(i + 1)), 2000m + i, "XAU"))
            .ToList();

        db.PriceHistory.AddRange(staleRecords);
        await db.SaveChangesAsync();

        db.PriceHistory.RemoveRange(staleRecords.Take(2));
        await db.SaveChangesAsync();

        var pruner = new PriceHistoryPruner();

        // Act
        var deletedCount = await pruner.PruneHourlyDataOlderThanAsync(db, cutoff, CancellationToken.None);

        // Assert
        Assert.Equal(3, deletedCount);
        Assert.Equal(0, await db.PriceHistory.CountAsync());
    }
}
