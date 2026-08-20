using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.Outbound.Data;

/// <summary>
/// Provides extension methods for automatic database migration and master data seeding on application startup using Entity Framework Core migrations.
/// Uses <see cref="DatabaseLogMessages"/> for zero-allocation logging.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Ensures that database schema and tables exist, pending EF Core migrations are applied, and master catalog (metals) is seeded.
    /// </summary>
    public static async Task ApplyMigrationsAndSeedAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ElementumDbContext>>();
        var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();

        try
        {
            if (db.Database.IsRelational())
            {
                try
                {
                    DatabaseLogMessages.ApplyingMigrations(logger);
                    await db.Database.MigrateAsync(cancellationToken);
                    DatabaseLogMessages.MigrationsAppliedSuccessfully(logger);
                }
                catch (InvalidOperationException ex)
                {
                    DatabaseLogMessages.RelationalMigrationSkipped(logger, ex);
                    await db.Database.EnsureCreatedAsync(cancellationToken);
                }
            }
            else
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }

            // Seed master data (metals) if not present
            if (!await db.Metals.AnyAsync(cancellationToken))
            {
                DatabaseLogMessages.SeedingMetalsCatalog(logger);
                db.Metals.AddRange(
                    new Metals { Id = 1, Symbol = "XAU", Name = "Gold" },
                    new Metals { Id = 2, Symbol = "XAG", Name = "Silver" },
                    new Metals { Id = 3, Symbol = "XPT", Name = "Platinum" },
                    new Metals { Id = 4, Symbol = "XPD", Name = "Palladium" }
                );
                await db.SaveChangesAsync(cancellationToken);
                DatabaseLogMessages.MetalsCatalogSeeded(logger);
            }
        }
        catch (Exception ex)
        {
            DatabaseLogMessages.MigrationError(logger, ex);
            throw;
        }
    }
}
