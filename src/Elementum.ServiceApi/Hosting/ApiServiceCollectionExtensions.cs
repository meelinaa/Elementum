using Elementum.Infrastructure.Data;
using Elementum_ServiceApi.Services;
using Elementum_ServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elementum_ServiceApi.Hosting;

/// <summary>
/// Registers all API dependencies: database, application services, OpenAPI, CORS, health checks, and request timeouts.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds Elementum Service API services to the DI container.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configuration">Application configuration (appsettings, environment variables).</param>
    public static void AddElementumApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["CONNECTION_STRING"]
            ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING.");

        // EF Core + MySQL with Polly retry for transient connection errors (see Infrastructure).
        services.AddElementumDbContext(connectionString, configureResilience: _ => { });

        services.AddScoped<IApiService, ApiService>();

        services.AddControllers();
        services.AddOpenApi();

        // RFC 7807 ProblemDetails for validation errors and exception handler integration.
        services.AddProblemDetails();

        // CORS: allowed origins from Cors:AllowedOrigins (array in appsettings); default for local SPA dev.
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

        // Kubernetes-style probes: "ready" tag limits which checks run on /health/ready.
        services.AddHealthChecks()
            .AddDbContextCheck<ElementumDbContext>("database", failureStatus: HealthStatus.Unhealthy, tags: new[] { "ready" });

        // Per-request timeouts: named policies for future endpoint-specific limits; default applies to all requests.
        services.AddRequestTimeouts(options =>
        {
            options.AddPolicy("Strict", TimeSpan.FromSeconds(5));
            options.AddPolicy("DataCruncher", TimeSpan.FromMinutes(1));

            options.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(30),
                WriteTimeoutResponse = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Timeout",
                        message = "The server took too long to respond."
                    });
                }
            };
        });
    }

    /// <summary>
    /// Configures Serilog as the sole logging provider, reading sinks and levels from configuration (e.g. appsettings.json).
    /// </summary>
    public static void UseElementumSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, _, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Elementum-ServiceApi"));
    }
}
