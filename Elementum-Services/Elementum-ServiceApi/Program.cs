using Elementum.Infrastructure.Data;
using Elementum_ServiceApi.Middleware;
using Elementum_ServiceApi.Services;
using Elementum_ServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json;

// Bootstrap Serilog so that startup and config loading can be logged.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog (structured logs, config from appsettings).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Elementum-ServiceApi"));

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["CONNECTION_STRING"]
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING.");
// Enable DB resilience: retry on transient MySQL errors (connection lost, deadlock). Options live in Infrastructure; Worker can use the same overload later.
builder.Services.AddElementumDbContext(connectionString, configureResilience: _ => { });
builder.Services.AddScoped<IApiService, ApiService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails(); // Provides standardized error responses for exceptions and non-successful HTTP status codes.

// CORS configuration to allow requests from the frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // TODO: needs to be changed to the actual frontend URL in production
              .AllowAnyMethod()       
              .AllowAnyHeader();                   
    });
});

// Health checks for monitoring the database connection. Tag "ready" so /health/ready runs this check.
builder.Services.AddHealthChecks()
                .AddDbContextCheck<ElementumDbContext>("database", failureStatus: HealthStatus.Unhealthy, tags: new[] { "ready" });

// Request timeouts: set a default timeout for all requests to prevent hanging requests from consuming resources indefinitely.
builder.Services.AddRequestTimeouts(options => {
    options.AddPolicy("Strict", TimeSpan.FromSeconds(5));

    options.AddPolicy("DataCruncher", TimeSpan.FromMinutes(1));

    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30),
        WriteTimeoutResponse = async (context) => {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Timeout",
                message = "The server took too long to respond."
            });
        }
    };
});

var app = builder.Build();

// Correlation ID and structured logging: run early so every log line includes CorrelationId.
app.UseMiddleware<CorrelationIdMiddleware>(); // Assigns a unique CorrelationId to each request (from header or new) and adds it to the logging context for traceability across logs.
app.UseRequestTimeouts();

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

// Global exception handler: unhandled exceptions return ProblemDetails JSON (no raw exception leak).
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exceptionHandlerFeature?.Error != null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "An error occurred",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = app.Environment.IsDevelopment() ? exceptionHandlerFeature.Error.Message : null,
                    Instance = $"{context.Request.Method} {context.Request.Path}"
                }
            });
        }
    });
});

// For development.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Provides detailed error pages for exceptions in development.
    app.MapOpenApi();
    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); // Allow all CORS requests in development for ease of testing with the frontend.
}
else // In production
{
    app.UseHttpsRedirection();
    app.UseCors("FrontendPolicy"); // Use the defined CORS policy in production to restrict access to the frontend URL.
}

app.UseAuthorization();
app.UseStatusCodePages(); // Return status code pages for non-successful HTTP responses (e.g. 404, 500) instead of empty responses.

// Health check endpoints: /health/live for liveness (always healthy) and /health/ready for readiness (checks database connection).
app.MapHealthChecks("/health/live", new HealthCheckOptions 
{ 
    Predicate = _ => false 
});
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

Log.Information("Elementum-ServiceApi started");
app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}