using Elementum.Domain.Entities;
using Elementum.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Elementum.IntegrationTests.Data;

[Collection(MySqlCollection.Name)]
public sealed class UniqueIndexMySqlTests : IAsyncLifetime
{
    private readonly string _connectionString;

    public UniqueIndexMySqlTests(MySqlContainerFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    public Task InitializeAsync() => MySqlTestContext.ResetSchemaAsync(_connectionString);

    public Task DisposeAsync() => Task.CompletedTask;

    // RIGHT-BIC[E]P: duplicate (MetalId, Currency, ReferenceTimestamp) is rejected by MySQL unique index
    [Fact]
    public async Task PriceHistory_DuplicateMetalCurrencyTimestamp_ThrowsDbUpdateException()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        db.PriceHistory.Add(PriceHistory.Create(1, "USD", date, 2500m, "XAU", referenceTimestamp: 1_723_900_000));
        await db.SaveChangesAsync();

        db.PriceHistory.Add(PriceHistory.Create(1, "USD", date, 2510m, "XAU", referenceTimestamp: 1_723_900_000));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.IsType<MySqlException>(ex.InnerException);
        Assert.Equal(1062, ((MySqlException)ex.InnerException!).Number);
    }

    // RIGHT-BIC[E]P: duplicate daily candle (MetalId, Currency, EntryDate) is rejected by unique index
    [Fact]
    public async Task DailyPriceSummary_DuplicateMetalCurrencyDate_ThrowsDbUpdateException()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        var date = new DateOnly(2026, 8, 21);

        db.DailyPriceSummaries.Add(DailyPriceSummary.Create(1, "EUR", date, 2200m, 2210m, 2190m, 2205m));
        await db.SaveChangesAsync();

        db.DailyPriceSummaries.Add(DailyPriceSummary.Create(1, "EUR", date, 2201m, 2211m, 2191m, 2206m));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.IsType<MySqlException>(ex.InnerException);
        Assert.Equal(1062, ((MySqlException)ex.InnerException!).Number);
    }

    // [R]IGHT-BICEP: the applied migration materializes the Fluent unique index on price_history
    [Fact]
    public async Task PriceHistory_UniqueIndex_ExistsOnMetalCurrencyTimestamp()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.statistics
            WHERE table_schema = DATABASE()
              AND LOWER(table_name) = 'price_history'
              AND non_unique = 0
              AND LOWER(index_name) <> 'primary'
              AND LOWER(column_name) IN ('metal_id', 'currency', 'reference_timestamp')
            """;

        var columnCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(3, columnCount);
    }

    // RIGHT-BIC[E]P: duplicate metals.symbol is rejected by the unique index
    [Fact]
    public async Task Metals_DuplicateSymbol_ThrowsDbUpdateException()
    {
        await using var db = MySqlTestContext.Create(_connectionString);

        db.Metals.Add(new Metals { Symbol = "XAU", Name = "Gold again" });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.IsType<MySqlException>(ex.InnerException);
        Assert.Equal(1062, ((MySqlException)ex.InnerException!).Number);
    }

    // [R]IGHT-BICEP: applied migrations map metals.symbol as unique varchar, not longtext
    [Fact]
    public async Task Metals_Symbol_IsUniqueVarchar()
    {
        await using var db = MySqlTestContext.Create(_connectionString);
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT column_type
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND LOWER(table_name) = 'metals'
              AND LOWER(column_name) = 'symbol'
            """;

        var columnType = Convert.ToString(await command.ExecuteScalarAsync());
        Assert.StartsWith("varchar(8)", columnType, StringComparison.OrdinalIgnoreCase);

        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.statistics
            WHERE table_schema = DATABASE()
              AND LOWER(table_name) = 'metals'
              AND non_unique = 0
              AND LOWER(index_name) <> 'primary'
              AND LOWER(column_name) = 'symbol'
            """;

        var uniqueColumns = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(1, uniqueColumns);
    }
}
