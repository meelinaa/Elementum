using Elementum.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json;

namespace Elementum.Api.Hosting;

/// <summary>
/// Configures the HTTP request pipeline: forwarded headers, correlation IDs, timeouts, logging, exception handling, CORS, rate limiting, and health endpoints.
/// </summary>
public static class ApiApplicationPipelineExtensions
{
    /// <summary>
    /// Registers middleware and endpoints for the Elementum REST API. Order matters (first registered = outermost for incoming requests).
    /// </summary>
    public static void UseElementumApiPipeline(this WebApplication app)
    {
        // Honor X-Forwarded-For / X-Forwarded-Proto from the TLS-terminating reverse proxy.
        app.UseForwardedHeaders();

        // Propagate or generate X-Correlation-ID for distributed tracing in logs.
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Enforce request timeout policies registered in DI.
        app.UseRequestTimeouts();

        // One-line HTTP request logs with optional CorrelationId enrichment.
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var correlationId = httpContext.Items[CorrelationIdMiddleware.HttpContextItemKey]?.ToString();
                if (!string.IsNullOrEmpty(correlationId))
                    diagnosticContext.Set("CorrelationId", correlationId);
            };
        });

        // Map unhandled exceptions to ProblemDetails via registered IExceptionHandler.
        app.UseExceptionHandler();

        // Enforce Content-Security-Policy, nosniff, and frame embedding protection across all API responses.
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none';");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "no-referrer");
            await next();
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            // Permissive CORS for local frontend tooling; production uses FrontendPolicy below.
            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        }
        else
        {
            app.UseHsts();
            app.UseCors("FrontendPolicy");
            // TLS terminates at the reverse proxy (Compose Caddy / load balancer). Do not redirect
            // HTTP→HTTPS inside the container — that would loop or fail on the internal HTTP port.
            var httpsTerminatesAtProxy = app.Configuration.GetValue("ReverseProxy:TerminateHttps", false);
            if (!httpsTerminatesAtProxy)
                app.UseHttpsRedirection();
        }

        // Apply global IP-based rate limiting (the abuse control for this public read API; there is no auth).
        app.UseRateLimiter();

        // Return a small HTML or plain body for 404/405 etc. instead of an empty response body.
        app.UseStatusCodePages();

        // Liveness: always 200 (no DB check) — used by orchestrators that only need process-up.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        // Readiness: runs checks tagged "ready" (database).
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    Status = report.Status.ToString(),
                    Checks = report.Entries.Select(e => new
                    {
                        Component = e.Key,
                        Status = e.Value.Status.ToString(),
                        e.Value.Description
                    }),
                    Duration = report.TotalDuration
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
            }
        });

        app.MapControllers();
    }
}
