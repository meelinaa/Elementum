using Elementum.Application.UseCases.Prices;
using Elementum.Domain.Ports;
using Elementum.Infrastructure.Caching;
using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Infrastructure.Data.Resilience;
using Elementum.Infrastructure.External;
using Elementum.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Elementum.Infrastructure.Data;

/// <summary>
/// Extension methods for registering Elementum Infrastructure, HybridCache, and secondary adapters in DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Elementum Infrastructure dependencies (DbContext, Ports, External API, Resiliency, HybridCache).
    /// </summary>
    public static IServiceCollection AddElementumInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Action<ElementumDbContextResilienceOptions>? configureResilience = null,
        string? redisConnectionString = null)
    {
        services.AddElementumDbContext(connectionString, configureResilience);

        // Register Driven / Secondary Ports
        services.AddScoped<IPriceHistoryRepository>(sp => sp.GetRequiredService<IElementumDbContext>());
        services.AddSingleton<IDatabaseCheckService, DatabaseCheckService>();
        services.AddSingleton<IMetalsApiClient, MetalsApiClient>();

        // Register HTTP client for GoldAPI with Polly transient retry policy
        services.AddHttpClient("GoldApi")
            .AddPolicyHandler(GetRetryPolicy());

        // Register HybridCache (L1 Memory + L2 Redis with Stampede Protection)
        services.AddElementumHybridCaching(redisConnectionString);

        return services;
    }

    /// <summary>
    /// Registers .NET 9/10 HybridCache with L1 (In-Memory) + optional L2 (Redis) and Stampede Protection.
    /// Decorates <see cref="IGetPriceHistoryUseCase"/> with <see cref="CachedGetPriceHistoryUseCase"/>.
    /// </summary>
    public static IServiceCollection AddElementumHybridCaching(
        this IServiceCollection services,
        string? redisConnectionString = null)
    {
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(60),
                LocalCacheExpiration = TimeSpan.FromSeconds(60)
            };
        });

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "Elementum:";
            });
        }

        // Decorate IGetPriceHistoryUseCase with CachedGetPriceHistoryUseCase
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IGetPriceHistoryUseCase));
        if (descriptor != null)
        {
            services.Remove(descriptor);
            services.Add(new ServiceDescriptor(
                typeof(GetPriceHistoryUseCase),
                descriptor.ImplementationType ?? typeof(GetPriceHistoryUseCase),
                descriptor.Lifetime));

            services.AddScoped<IGetPriceHistoryUseCase>(sp =>
            {
                var inner = sp.GetRequiredService<GetPriceHistoryUseCase>();
                var cache = sp.GetRequiredService<HybridCache>();
                return new CachedGetPriceHistoryUseCase(inner, cache);
            });
        }

        return services;
    }

    /// <summary>
    /// Registers <see cref="ElementumDbContext"/> and <see cref="IElementumDbContext"/>.
    /// </summary>
    public static IServiceCollection AddElementumDbContext(
        this IServiceCollection services,
        string connectionString,
        Action<ElementumDbContextResilienceOptions>? configureResilience = null)
    {
        services.AddDbContext<ElementumDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

        if (configureResilience != null)
        {
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

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
