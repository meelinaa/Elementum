using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Ports.Outbound;
using Elementum.Application.Services;
using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Outbound.Caching;
using Elementum.Infrastructure.Outbound.Data.Repositories;
using Elementum.Infrastructure.Outbound.Data.Resilience;
using Elementum.Infrastructure.Outbound.Data.Services;
using Elementum.Infrastructure.Outbound.External;
using Elementum.Infrastructure.Outbound.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elementum.Infrastructure.Outbound.Data;

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

        services.AddSingleton<IMetalsApiClient, MetalsApiClient>();
        services.AddSingleton<IDistributedLockProvider, EfCoreDistributedLockProvider>();

        services.AddMemoryCache();

        // Register HTTP client for GoldAPI with Polly v8 standard resilience pipeline (Timeout + Retry + Circuit Breaker)
        services.AddHttpClient("GoldApi")
            .AddStandardResilienceHandler(options =>
            {
                // Total request timeout (outer): 30s (default) — keeps the overall cap.
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

                // Per-attempt timeout: 10s (matches the previous Polly 7 timeout policy).
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

                // Retry: 3 attempts, exponential backoff with jitter (same semantics as before).
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.UseJitter = true;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.Delay = TimeSpan.FromSeconds(2);

                // Circuit breaker: open after 5 failures within a 1-minute sampling window.
                options.CircuitBreaker.FailureRatio = 1.0;   // trip on consecutive failures
                options.CircuitBreaker.MinimumThroughput = 5; // at least 5 calls before tripping
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
            });

        // Register HybridCache (L1 Memory + L2 Redis with Stampede Protection)
        services.AddElementumHybridCaching(redisConnectionString);

        return services;
    }

    /// <summary>
    /// Registers HybridCache with L1 (In-Memory) + optional L2 (Redis) and Stampede Protection.
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

        // Design Decision: Manual Decorator Registration (services.FirstOrDefault / Remove / Add)
        // RATIONALE vs. Scrutor:
        // 1. Dependency Minimization: Avoids an external 3rd-party package dependency (Scrutor) for two decorators.
        // 2. Deterministic & Transparent: Explicit compile-time wiring without runtime assembly scanning or reflection overhead.
        // 3. Native DI Compatibility: Works natively on Microsoft.Extensions.DependencyInjection with full control over inner lifetime.
        //
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

        // Decorate ILiveQuotesProvider with CachedLiveQuotesProvider
        var liveQuotesDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILiveQuotesProvider));
        if (liveQuotesDescriptor != null)
        {
            services.Remove(liveQuotesDescriptor);
            services.Add(new ServiceDescriptor(
                typeof(LiveQuotesProvider),
                liveQuotesDescriptor.ImplementationType ?? typeof(LiveQuotesProvider),
                liveQuotesDescriptor.Lifetime));

            services.AddScoped<ILiveQuotesProvider>(sp =>
            {
                var inner = sp.GetRequiredService<LiveQuotesProvider>();
                var cache = sp.GetRequiredService<IMemoryCache>();
                return new CachedLiveQuotesProvider(inner, cache);
            });
        }

        return services;
    }

    /// <summary>
    /// Registers <see cref="ElementumDbContext"/>, repository, services, and <see cref="IPriceHistoryRepository"/>.
    /// </summary>
    public static IServiceCollection AddElementumDbContext(
        this IServiceCollection services,
        string connectionString,
        Action<ElementumDbContextResilienceOptions>? configureResilience = null)
    {
        services.AddDbContext<ElementumDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

        services.AddScoped<IDailyCandleAggregator, DailyCandleAggregator>();
        services.AddScoped<IPriceHistoryPruner, PriceHistoryPruner>();
        services.AddScoped<PriceHistoryRepository>();

        if (configureResilience != null)
        {
            services.Configure(configureResilience);
            services.AddScoped<IPriceHistoryReadRepository>(sp =>
            {
                var inner = sp.GetRequiredService<PriceHistoryRepository>();
                var opts = sp.GetRequiredService<IOptions<ElementumDbContextResilienceOptions>>().Value;
                var pipeline = DatabaseResiliencePolicy.BuildRetryPipeline(opts);
                return new ResilientPriceHistoryReadRepository(inner, pipeline);
            });
            services.AddScoped<IPriceHistoryWriteRepository>(sp =>
            {
                var inner = sp.GetRequiredService<PriceHistoryRepository>();
                var opts = sp.GetRequiredService<IOptions<ElementumDbContextResilienceOptions>>().Value;
                var pipeline = DatabaseResiliencePolicy.BuildRetryPipeline(opts);
                return new ResilientPriceHistoryWriteRepository(inner, pipeline);
            });
            services.AddScoped<IPriceHistoryRepository>(sp =>
            {
                var read = sp.GetRequiredService<IPriceHistoryReadRepository>();
                var write = sp.GetRequiredService<IPriceHistoryWriteRepository>();
                return new ResilientElementumDbContext(read, write);
            });
        }
        else
        {
            services.AddScoped<IPriceHistoryReadRepository, PriceHistoryRepository>();
            services.AddScoped<IPriceHistoryWriteRepository, PriceHistoryRepository>();
            services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
        }

        return services;
    }
}
