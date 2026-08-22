using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data;
using Microsoft.EntityFrameworkCore;

namespace Elementum.IntegrationTests.Fixtures;

internal static class MySqlTestContext
{
    public static readonly MySqlServerVersion ServerVersion = new(new Version(8, 0, 36));

    public static ElementumDbContext Create(string connectionString) =>
        new(new DbContextOptionsBuilder<ElementumDbContext>()
            .UseMySql(connectionString, ServerVersion)
            .Options);

    public static async Task DropAllTablesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var db = Create(connectionString);
        await db.Database.OpenConnectionAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();

        var tableNames = new List<string>();
        await using (var list = connection.CreateCommand())
        {
            list.CommandText = """
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = DATABASE()
                """;
            await using var reader = await list.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                tableNames.Add(reader.GetString(0));
        }

        if (tableNames.Count == 0)
            return;

        await using var drop = connection.CreateCommand();
        drop.CommandText =
            "SET FOREIGN_KEY_CHECKS = 0; "
            + string.Join(' ', tableNames.Select(name => $"DROP TABLE IF EXISTS `{name}`;"))
            + " SET FOREIGN_KEY_CHECKS = 1;";
        await drop.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task ResetSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await DropAllTablesAsync(connectionString, cancellationToken);
        await using var db = Create(connectionString);
        await db.Database.MigrateAsync(cancellationToken);
        await SeedMetalsAsync(db, cancellationToken);
    }

    public static async Task SeedMetalsAsync(ElementumDbContext db, CancellationToken cancellationToken = default)
    {
        db.Metals.AddRange(
            new Metals { Id = 1, Symbol = "XAU", Name = "Gold" },
            new Metals { Id = 2, Symbol = "XAG", Name = "Silver" },
            new Metals { Id = 3, Symbol = "XPT", Name = "Platinum" },
            new Metals { Id = 4, Symbol = "XPD", Name = "Palladium" });
        await db.SaveChangesAsync(cancellationToken);
    }
}
