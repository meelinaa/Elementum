using Elementum.Domain.Entities;
using Elementum.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.Outbound.Data;

/// <summary>
/// Provides extension methods for automatic database migration and master data seeding on application startup using Entity Framework Core migrations.
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
                    logger.LogInformation("Applying pending Entity Framework Core migrations...");
                    await db.Database.MigrateAsync(cancellationToken);
                    logger.LogInformation("Entity Framework Core migrations applied successfully.");
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning(ex, "Relational migration skipped (non-relational or mock provider detected). Ensuring database created.");
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
                logger.LogInformation("Seeding master metals catalog (Gold, Silver, Platinum, Palladium)...");
                db.Metals.AddRange(
                    new Metals { Id = 1, Symbol = "XAU", Name = "Gold" },
                    new Metals { Id = 2, Symbol = "XAG", Name = "Silver" },
                    new Metals { Id = 3, Symbol = "XPT", Name = "Platinum" },
                    new Metals { Id = 4, Symbol = "XPD", Name = "Palladium" }
                );
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Metals master catalog seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Entity Framework Core database migration and schema initialization.");
            throw;
        }
    }
}
