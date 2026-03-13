using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Infrastructure.Data.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elementum.Infrastructure.Data;

/// <summary>
/// Extension methods for registering Elementum data access in the DI container.
/// Supports optional resilience (retry on transient MySQL errors); same extension is used by API and Worker.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ElementumDbContext"/> with the MySQL provider and <see cref="IElementumDbContext"/>.
    /// When <paramref name="configureResilience"/> is set, <see cref="IElementumDbContext"/> is resolved as <see cref="ResilientElementumDbContext"/> (retry on transient DB errors); otherwise the concrete context is returned.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">MySQL connection string (Server=...;Port=3306;Database=...;User=...;Password=...).</param>
    /// <param name="configureResilience">Optional. When not null, configures retry options and registers the resilient decorator. Use e.g. <c>_ => { }</c> for defaults, or <c>o => { o.MaxRetryCount = 5; }</c> to override.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddElementumDbContext(
        this IServiceCollection services,
        string connectionString,
        Action<ElementumDbContextResilienceOptions>? configureResilience = null)
    {
        services.AddDbContext<ElementumDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        if (configureResilience != null)
        {
            // Register options so IOptions&lt;ElementumDbContextResilienceOptions&gt; is available; then resolve IElementumDbContext as the retry decorator.
            services.Configure(configureResilience);
            services.AddScoped<IElementumDbContext>(sp =>
            {
                var inner = sp.GetRequiredService<ElementumDbContext>();
                var opts = sp.GetRequiredService<IOptions<ElementumDbContextResilienceOptions>>().Value;
                var policy = DatabaseResiliencePolicy.BuildRetryPolicy(opts);
                return new ResilientElementumDbContext(inner, policy);
            });
        }
        else
        {
            services.AddScoped<IElementumDbContext>(sp => sp.GetRequiredService<ElementumDbContext>());
        }

        return services;
    }
}
