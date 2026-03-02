using Elementum.Infrastructure.Data;
using Elementum_ServiceApi.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["CONNECTION_STRING"]
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING.");
builder.Services.AddElementumDbContext(connectionString);
builder.Services.AddScoped<ApiService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

// For development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}
else // In production
{
    app.UseCors("FrontendPolicy");
}

app.UseHttpsRedirection();
app.UseAuthorization();

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