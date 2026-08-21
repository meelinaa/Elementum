using Elementum.Infrastructure.Outbound.Data;
using Elementum.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elementum.IntegrationTests.Data;

[Collection(MySqlCollection.Name)]
public sealed class MigrationMySqlTests
{
    private readonly string _connectionString;

    public MigrationMySqlTests(MySqlContainerFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    // [R]IGHT-BICEP: EF ApplyMigrationsAndSeedAsync applies pending migrations and seeds the metals catalog
    [Fact]
    public async Task ApplyMigrationsAndSeedAsync_OnEmptyDatabase_AppliesInitialCreateAndSeedsMetals()
    {
        await MySqlTestContext.DropAllTablesAsync(_connectionString);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<ElementumDbContext>(options =>
            options.UseMySql(_connectionString, MySqlTestContext.ServerVersion));
        await using var provider = services.BuildServiceProvider();

        await provider.ApplyMigrationsAndSeedAsync();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();

        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Contains(applied, name => name.Contains("InitialCreate", StringComparison.Ordinal));
        Assert.Equal(4, await db.Metals.CountAsync());
        Assert.Contains(await db.Metals.ToListAsync(), m => m.Symbol == "XAU");
    }

    // [R]IGHT-BICEP: MigrateAsync creates the lock and price tables from the migrations assembly
    [Fact]
    public async Task MigrateAsync_CreatesExpectedTables()
    {
        await MySqlTestContext.DropAllTablesAsync(_connectionString);

        await using var db = MySqlTestContext.Create(_connectionString);
        await db.Database.MigrateAsync();

        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND LOWER(table_name) IN ('metals', 'price_history', 'daily_price_summaries', 'distributed_locks', '__efmigrationshistory')
            """;

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));

        Assert.Contains("metals", tables);
        Assert.Contains("price_history", tables);
        Assert.Contains("daily_price_summaries", tables);
        Assert.Contains("distributed_locks", tables);
        Assert.Contains("__efmigrationshistory", tables);
    }
}
