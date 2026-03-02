using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elementum.Infrastructure.Data;

/// <summary>
/// Extension methods for registering Elementum data access in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ElementumDbContext"/> with the MySQL provider. Use the same connection string key as in config (e.g. ConnectionStrings:DefaultConnection or CONNECTION_STRING in .env).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">MySQL connection string (Server=...;Port=3306;Database=...;User=...;Password=...).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddElementumDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ElementumDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        return services;
    }
}
