using Elementum.Application;
using Elementum.Infrastructure.Data;
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
    /// <summary>
    /// Adds Elementum API services to the DI container.
    /// </summary>
    public static void AddElementumApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["CONNECTION_STRING"]
            ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING.");

        // Infrastructure: EF Core + MySQL with Polly retry and secondary adapters
        services.AddElementumInfrastructure(connectionString, configureResilience: _ => { });

        // Application: Use Cases and Interactors
        services.AddElementumApplication();

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
            options.AddPolicy("Strict", TimeSpan.FromSeconds(5));
            options.AddPolicy("DataCruncher", TimeSpan.FromMinutes(1));

            options.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(30),
                TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
            };
        });
    }
}
