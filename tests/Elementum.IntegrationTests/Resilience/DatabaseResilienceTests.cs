using System.Net.Sockets;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Outbound.Data;
using Elementum.Infrastructure.Outbound.Data.Repositories;
using Elementum.Infrastructure.Outbound.Data.Resilience;
using Elementum.Infrastructure.Outbound.Data.Services;
using Elementum.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Elementum.IntegrationTests.Resilience;

[Collection(MySqlCollection.Name)]
public sealed class DatabaseResilienceTests : IAsyncLifetime
{
    private readonly string _connectionString;

    public DatabaseResilienceTests(MySqlContainerFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    public Task InitializeAsync() => MySqlTestContext.ResetSchemaAsync(_connectionString);

    public Task DisposeAsync() => Task.CompletedTask;

    // [R]IGHT-BICEP: production DI wires ResilientElementumDbContext and reads from MySQL
    [Fact]
    public async Task AddElementumDbContext_WithResilience_ReadsMetalsFromMySql()
    {
        await using var provider = CreateResilientProvider(_connectionString);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IPriceHistoryRepository>();

        Assert.IsType<ResilientElementumDbContext>(db);
        var metals = await db.GetMetalsAsync();

        Assert.Equal(4, metals.Count);
        Assert.Contains(metals, m => m.Symbol == "XAU");
    }

    // [R]IGHT-BICEP: latest-tick query through the Polly decorator hits the real table
    [Fact]
    public async Task GetPriceHistoryAllLatest_ThroughDecorator_ReturnsPersistedTick()
    {
        await using var seed = MySqlTestContext.Create(_connectionString);
        seed.PriceHistory.Add(PriceHistory.Create(
            metalId: 1,
            currency: "USD",
            entryDate: DateOnly.FromDateTime(DateTime.UtcNow),
            price: 2500m,
            symbol: "XAU",
            referenceTimestamp: 1_723_900_000));
        await seed.SaveChangesAsync();

        await using var db = MySqlTestContext.Create(_connectionString);
        var resilient = CreateResilientContext(db);

        var latest = (await resilient.GetPriceHistoryAllLatest(CancellationToken.None)).ToList();

        var tick = Assert.Single(latest);
        Assert.Equal(1, tick.MetalId);
        Assert.Equal(2500m, tick.Price);
    }

    // [E]RROR RIGHT-BICEP: a killed MySQL session is retried on a new connection and then succeeds
    [Fact]
    public async Task GetMetalsAsync_WhenConnectionKilled_RetriesAndSucceeds()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        await db.Database.OpenConnectionAsync();
        var connectionId = await ReadConnectionIdAsync(db);

        await using var killer = MySqlTestContext.Create(_connectionString);
        await killer.Database.ExecuteSqlAsync($"KILL {connectionId}");

        var resilient = CreateResilientContext(db, maxRetryCount: 3);

        var metals = await resilient.GetMetalsAsync();

        Assert.Equal(4, metals.Count);
    }

    // [E]RROR RIGHT-BICEP: an unreachable MySQL endpoint exhausts retries and surfaces a transient transport error
    [Fact]
    public async Task GetMetalsAsync_WhenMySqlUnreachable_PropagatesAfterRetries()
    {
        var unreachable = new MySqlConnectionStringBuilder(_connectionString)
        {
            Server = "127.0.0.1",
            Port = 1,
            ConnectionTimeout = 1
        }.ConnectionString;

        await using var db = MySqlTestContext.Create(unreachable);
        var resilient = CreateResilientContext(db, maxRetryCount: 1);

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => resilient.GetMetalsAsync());

        Assert.True(
            DatabaseResiliencePolicy.IsTransientException(ex),
            $"Expected a transient transport error after retries, got {ex.GetType().FullName}: {ex.Message}");
        Assert.True(ex is MySqlException or SocketException or TimeoutException
                    || ex.InnerException is MySqlException or SocketException);
    }

    private static ResilientElementumDbContext CreateResilientContext(
        ElementumDbContext db,
        int maxRetryCount = 3)
    {
        var repository = new PriceHistoryRepository(db, new DailyCandleAggregator(), new PriceHistoryPruner());
        var pipeline = DatabaseResiliencePolicy.BuildRetryPipeline(new ElementumDbContextResilienceOptions
        {
            MaxRetryCount = maxRetryCount,
            InitialDelay = TimeSpan.FromMilliseconds(20),
            UseExponentialBackoff = false
        });
        return new ResilientElementumDbContext(repository, pipeline);
    }

    private static ServiceProvider CreateResilientProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddElementumDbContext(connectionString, _ => { });
        return services.BuildServiceProvider();
    }

    private static async Task<long> ReadConnectionIdAsync(ElementumDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT CONNECTION_ID()";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
