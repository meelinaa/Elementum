using System.Threading.RateLimiting;
using Elementum.Api.Exceptions;
using Elementum.Application;
using Elementum.Application.Options;
using Elementum.Infrastructure.Outbound.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elementum.Api.Hosting;

/// <summary>
/// Registers all API dependencies: database, application use cases, OpenAPI, CORS, health checks, rate limiting, and request timeouts.
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
            throw Elementum.Application.Exceptions.ConfigurationException.MissingConnectionString("DefaultConnection");
        }

        // Fail-Fast Options Validation on Start
        services.AddOptions<MetalsApiOptions>()
            .BindConfiguration(MetalsApiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
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
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Rate Limiting: IP-based partition with 429 ProblemDetails rejection handler
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.20",
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Rate limit exceeded. Please try again later.",
                    Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}"
                };
                await context.HttpContext.Response.WriteAsJsonAsync(
                    problemDetails,
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                    ?? "anonymous";

                var rateLimitConfig = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                    ?? new RateLimitingOptions();

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitConfig.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitConfig.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = rateLimitConfig.QueueLimit
                    });
            });
        });

        // CORS: Development uses AllowAnyOrigin in the pipeline; Production uses this named policy.
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is not { Length: > 0 })
            allowedOrigins = ["http://localhost:3000"];

        services.AddCors(options =>
        {
            options.AddPolicy("FrontendPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Reverse proxy (Compose Caddy / cloud LB) terminates TLS and sets X-Forwarded-*.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
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
