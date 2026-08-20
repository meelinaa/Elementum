using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.Outbound.Data.Logging;

/// <summary>
/// Source-generated logging for database migrations and catalog seeding.
/// </summary>
public static partial class DatabaseLogMessages
{
    [LoggerMessage(
        EventId = 3202,
        Level = LogLevel.Information,
        Message = "Applying pending Entity Framework Core migrations...")]
    public static partial void ApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 3203,
        Level = LogLevel.Information,
        Message = "Entity Framework Core migrations applied successfully.")]
    public static partial void MigrationsAppliedSuccessfully(ILogger logger);

    [LoggerMessage(
        EventId = 3204,
        Level = LogLevel.Warning,
        Message = "Relational migration skipped (non-relational or mock provider detected). Ensuring database created.")]
    public static partial void RelationalMigrationSkipped(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 3205,
        Level = LogLevel.Information,
        Message = "Seeding master metals catalog (Gold, Silver, Platinum, Palladium)...")]
    public static partial void SeedingMetalsCatalog(ILogger logger);

    [LoggerMessage(
        EventId = 3206,
        Level = LogLevel.Information,
        Message = "Metals master catalog seeded successfully.")]
    public static partial void MetalsCatalogSeeded(ILogger logger);

    [LoggerMessage(
        EventId = 3207,
        Level = LogLevel.Error,
        Message = "Error during Entity Framework Core database migration and schema initialization.")]
    public static partial void MigrationError(ILogger logger, Exception ex);
}
