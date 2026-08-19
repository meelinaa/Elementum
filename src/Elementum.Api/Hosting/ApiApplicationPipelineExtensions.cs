using Elementum.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json;

namespace Elementum.Api.Hosting;

/// <summary>
/// Configures the HTTP request pipeline: correlation IDs, timeouts, logging, exception handling, CORS, rate limiting, authorization, and health endpoints.
/// </summary>
public static class ApiApplicationPipelineExtensions
{
    /// <summary>
    /// Registers middleware and endpoints for the Elementum REST API. Order matters (first registered = outermost for incoming requests).
    /// </summary>
    public static void UseElementumApiPipeline(this WebApplication app)
    {
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

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            // Permissive CORS for local frontend tooling; production uses FrontendPolicy below.
            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseCors("FrontendPolicy");
        }

        // Apply global IP-based rate limiting
        app.UseRateLimiter();

        app.UseAuthorization();

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
                        Description = e.Value.Description
                    }),
                    Duration = report.TotalDuration
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
            }
        });

        app.MapControllers();
    }
}
