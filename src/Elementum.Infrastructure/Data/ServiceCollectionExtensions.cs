using Elementum.Domain.Ports;
using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Infrastructure.Data.Resilience;
using Elementum.Infrastructure.External;
using Elementum.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Elementum.Infrastructure.Data;

/// <summary>
/// Extension methods for registering Elementum Infrastructure and secondary adapters in DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Elementum Infrastructure dependencies (DbContext, Ports, External API, Resiliency).
    /// </summary>
    public static IServiceCollection AddElementumInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Action<ElementumDbContextResilienceOptions>? configureResilience = null)
    {
        services.AddElementumDbContext(connectionString, configureResilience);

        // Register Driven / Secondary Ports
        services.AddScoped<IPriceHistoryRepository>(sp => sp.GetRequiredService<IElementumDbContext>());
        services.AddSingleton<IDatabaseCheckService, DatabaseCheckService>();
        services.AddSingleton<IMetalsApiClient, MetalsApiClient>();

        // Register HTTP client for GoldAPI with Polly transient retry policy
        services.AddHttpClient("GoldApi")
            .AddPolicyHandler(GetRetryPolicy());

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
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

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
