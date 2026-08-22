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
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
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

    // [R]IGHT-BICEP: InitialCreate matches the Fluent model (bigint timestamp, varchar(3) currency, decimal(18,4), no karat/ask/bid)
    [Fact]
    public async Task MigrateAsync_CreatesPriceHistoryColumnsMatchingCurrentModel()
    {
        await MySqlTestContext.DropAllTablesAsync(_connectionString);

        await using var db = MySqlTestContext.Create(_connectionString);
        await db.Database.MigrateAsync();
        await db.Database.OpenConnectionAsync();

        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT column_name, column_type
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND LOWER(table_name) = 'price_history'
            """;

        var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                columns[reader.GetString(0)] = reader.GetString(1);
        }

        Assert.Equal("bigint", columns["reference_timestamp"], StringComparer.OrdinalIgnoreCase);
        Assert.StartsWith("varchar(3)", columns["currency"], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("decimal(18,4)", columns["price"], StringComparer.OrdinalIgnoreCase);
        Assert.False(columns.ContainsKey("Ask"));
        Assert.False(columns.ContainsKey("Bid"));
        Assert.False(columns.ContainsKey("Exchange"));
        Assert.False(columns.ContainsKey("price_gram_24k"));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    // [R]IGHT-BICEP: the applied InitialCreate creates UNIQUE (metal_id, currency, reference_timestamp)
    [Fact]
    public async Task MigrateAsync_CreatesUniqueIndexOnMetalCurrencyReferenceTimestamp()
    {
        await MySqlTestContext.DropAllTablesAsync(_connectionString);

        await using var db = MySqlTestContext.Create(_connectionString);
        await db.Database.MigrateAsync();
        await db.Database.OpenConnectionAsync();

        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT LOWER(column_name)
            FROM information_schema.statistics
            WHERE table_schema = DATABASE()
              AND LOWER(table_name) = 'price_history'
              AND LOWER(index_name) = 'ix_price_history_metal_id_currency_reference_timestamp'
              AND non_unique = 0
            ORDER BY seq_in_index
            """;

        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            columns.Add(reader.GetString(0));

        Assert.Equal(["metal_id", "currency", "reference_timestamp"], columns);
    }
}
