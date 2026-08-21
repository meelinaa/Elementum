using Elementum.Application.Mapping;
using Elementum.Application.Models;
using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data;
using Elementum.Infrastructure.Outbound.Data.Repositories;
using Elementum.Infrastructure.Outbound.Data.Services;
using Elementum.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Elementum.IntegrationTests.Data;

[Collection(MySqlCollection.Name)]
public sealed class PriceHistoryRepositoryMySqlTests : IAsyncLifetime
{
    private readonly string _connectionString;

    public PriceHistoryRepositoryMySqlTests(MySqlContainerFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    public Task InitializeAsync() => MySqlTestContext.ResetSchemaAsync(_connectionString);

    public Task DisposeAsync() => Task.CompletedTask;

    // [I]NVERSE / IDEMPOTENCY: MySQL upsert keeps a single tick per (metal, currency, timestamp)
    [Fact]
    public async Task SavePricesAsync_WhenRunTwiceWithSameTimestamp_DoesNotCreateDuplicateTicks()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        var repository = new PriceHistoryRepository(db, new DailyCandleAggregator(), new PriceHistoryPruner());
        var entities = CreateTickSet(db, timestamp: 1_723_900_000);

        await repository.SavePricesAsync(entities);
        var countAfterFirstRun = await db.PriceHistory.CountAsync();

        await repository.SavePricesAsync(entities);
        var countAfterSecondRun = await db.PriceHistory.CountAsync();

        Assert.Equal(8, countAfterFirstRun);
        Assert.Equal(8, countAfterSecondRun);
    }

    // [B]OUNDARY: a different ReferenceTimestamp on the same day inserts additional ticks
    [Fact]
    public async Task SavePricesAsync_WhenSameDayButDifferentTimestamp_CreatesAdditionalTicks()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        var repository = new PriceHistoryRepository(db, new DailyCandleAggregator(), new PriceHistoryPruner());
        var first = CreateTickSet(db, timestamp: 1_723_900_000);
        var second = CreateTickSet(db, timestamp: 1_723_903_600);

        await repository.SavePricesAsync(first);
        await repository.SavePricesAsync(second);

        Assert.Equal(16, await db.PriceHistory.CountAsync());
    }

    // [R]IGHT-BICEP: MySQL latest query uses MAX(reference_timestamp), so a later insert with an older tick is ignored
    [Fact]
    public async Task GetPriceHistoryAllLatest_WhenNewerIdHasOlderTimestamp_ReturnsMaxReferenceTimestamp()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        var repository = new PriceHistoryRepository(db, new DailyCandleAggregator(), new PriceHistoryPruner());
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        db.PriceHistory.Add(PriceHistory.Create(1, "USD", date, 2500m, "XAU", referenceTimestamp: 200L));
        await db.SaveChangesAsync();
        db.PriceHistory.Add(PriceHistory.Create(1, "USD", date, 2400m, "XAU", referenceTimestamp: 100L));
        await db.SaveChangesAsync();

        var latest = await repository.GetPriceHistoryAllLatest(CancellationToken.None);

        var tick = Assert.Single(latest);
        Assert.Equal(2500m, tick.Price);
        Assert.Equal(200L, tick.ReferenceTimestamp);
    }

    // [R]IGHT-BICEP: bulk upsert updates the existing tick price without inserting a second row
    [Fact]
    public async Task SavePricesAsync_WhenSameKeyWithNewPrice_UpdatesExistingRow()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        var repository = new PriceHistoryRepository(db, new DailyCandleAggregator(), new PriceHistoryPruner());
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        await repository.SavePricesAsync(
            [PriceHistory.Create(1, "USD", date, 2500m, "XAU", referenceTimestamp: 1_723_900_000)]);
        await repository.SavePricesAsync(
            [PriceHistory.Create(1, "USD", date, 2510m, "XAU", referenceTimestamp: 1_723_900_000)]);

        var row = Assert.Single(await db.PriceHistory.ToListAsync());
        Assert.Equal(2510m, row.Price);
    }

    // [B]OUNDARY: ExecuteDeleteAsync removes only ticks strictly older than the cutoff date
    [Fact]
    public async Task PruneHourlyDataOlderThanAsync_DeletesRowsOlderThanCutoffViaExecuteDelete()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        var repository = new PriceHistoryRepository(db, new DailyCandleAggregator(), new PriceHistoryPruner());
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var cutoffDate = DateOnly.FromDateTime(cutoff);

        db.PriceHistory.AddRange(
            PriceHistory.Create(1, "USD", cutoffDate.AddDays(-1), 2000m, "XAU", referenceTimestamp: 1L),
            PriceHistory.Create(1, "USD", cutoffDate, 2100m, "XAU", referenceTimestamp: 2L),
            PriceHistory.Create(1, "USD", cutoffDate.AddDays(1), 2200m, "XAU", referenceTimestamp: 3L));
        await db.SaveChangesAsync();

        var deleted = await repository.PruneHourlyDataOlderThanAsync(cutoff);

        Assert.Equal(1, deleted);
        var remaining = await db.PriceHistory.AsNoTracking().ToListAsync();
        Assert.Equal(2, remaining.Count);
        Assert.DoesNotContain(remaining, p => p.EntryDate < cutoffDate);
    }

    private static IReadOnlyList<PriceHistory> CreateTickSet(ElementumDbContext db, long timestamp)
    {
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
            Timestamp = timestamp
        };

        var metalsMap = db.Metals.ToDictionary(m => m.Symbol, m => m.Id);
        return payload.ToPriceHistoryEntities(metalsMap, DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
