using Elementum.Application;
using Elementum.Application.Options;
using Elementum.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elementum.Api.Hosting;

/// <summary>
/// Registers all API dependencies: database, application use cases, OpenAPI, CORS, health checks, and request timeouts.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    private static readonly TimeSpan StrictTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DataCruncherTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Adds Elementum API services to the DI container with fail-fast options validation.
    /// </summary>
    public static void AddElementumApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING in appsettings.json or environment variables.");
        }

        // Fail-Fast Options Validation on Start
        services.AddOptions<MetalsApiOptions>()
            .BindConfiguration(MetalsApiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Application: Use Cases and Interactors
        services.AddElementumApplication();

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["REDIS_CONNECTION_STRING"];

        // Infrastructure: EF Core + MySQL with Polly retry, secondary adapters, and HybridCache
        services.AddElementumInfrastructure(connectionString, configureResilience: _ => { }, redisConnectionString);

        services.AddControllers(options =>
        {
            options.Filters.Add<Filters.ValidationFilter>();
        });
        services.AddOpenApi();

        // RFC 7807 ProblemDetails for validation errors and exception handler integration.
        services.AddProblemDetails();

        // CORS
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
        services.AddCors(options =>
        {
            options.AddPolicy("FrontendPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Kubernetes-style probes
        services.AddHealthChecks()
            .AddDbContextCheck<ElementumDbContext>("database", failureStatus: HealthStatus.Unhealthy, tags: new[] { "ready" });

        // Per-request timeouts
        services.AddRequestTimeouts(options =>
        {
            options.AddPolicy("Strict", StrictTimeout);
            options.AddPolicy("DataCruncher", DataCruncherTimeout);

            options.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = DefaultTimeout,
                TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
            };
        });
    }
}
