using Elementum.Infrastructure.Data;
using Elementum_ServiceApi.Services;
using Elementum_ServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["CONNECTION_STRING"]
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING.");
builder.Services.AddElementumDbContext(connectionString);
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

// Health checks for monitoring the database connection.
builder.Services.AddHealthChecks()
                .AddDbContextCheck<ElementumDbContext>("Database");

var app = builder.Build();

// Global exception handler: unhandled exceptions return ProblemDetails JSON (no raw exception leak).
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var problemDetailsService = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Mvc.Infrastructure.IProblemDetailsService>();
        var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exceptionHandlerFeature?.Error != null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await problemDetailsService.WriteAsync(new Microsoft.AspNetCore.Mvc.ProblemDetailsContext
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
    app.UseCors("FrontendPolicy"); // Use the defined CORS policy in production to restrict access to the frontend URL.
}

app.UseHttpsRedirection();
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

app.Run();