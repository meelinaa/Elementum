using Elementum.Application;
using Elementum.Application.Options;
using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.External;
using Elementum.Worker.Jobs;
using Elementum.Worker.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Elementum.Worker.Hosting;

/// <summary>
/// Configures the Worker as a minimal ASP.NET Core host, Serilog, Infrastructure, Application Use Cases, and Background Ingestion.
/// </summary>
public static class WorkerHostBuilderExtensions
{
    /// <summary>
    /// Binds Kestrel URLs (defaults to port 5094).
    /// </summary>
    public static void ConfigureWorkerListening(this WebApplicationBuilder builder)
    {
        var workerUrls = builder.Configuration["Urls"] ?? "http://localhost:5094";
        builder.WebHost.UseUrls(workerUrls);
    }

    /// <summary>
    /// Configures Serilog.
    /// </summary>
    public static void ConfigureWorkerSerilog(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);
    }

    /// <summary>
    /// Registers Infrastructure, Application, health checks, ingestion job, hosted <see cref="Worker"/>, and options.
    /// </summary>
    public static void AddWorkerApplicationServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING in appsettings.json or .env.");

        // Infrastructure & Application
        services.AddElementumInfrastructure(connectionString);
        services.AddElementumApplication();

        services.AddHealthChecks()
            .AddDbContextCheck<ElementumDbContext>("database", failureStatus: HealthStatus.Unhealthy, tags: new[] { "ready" })
            .AddCheck<GoldApiHealthCheck>("goldapi", failureStatus: HealthStatus.Degraded, tags: new[] { "ready" });

        services.AddSingleton<IngestionMetrics>();
        services.AddScoped<MetalsIngestionJob>();
        services.AddHostedService<Elementum.Worker.Worker>();

        builder.Host.UseWindowsService();

        ConfigureMetalsApiOptions(services, configuration);
        services.Configure<WorkerScheduleOptions>(configuration.GetSection(WorkerScheduleOptions.SectionName));
    }

    private static void ConfigureMetalsApiOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MetalsApiOptions>(options =>
        {
            var section = configuration.GetSection(MetalsApiOptions.SectionName);
            section.Bind(options);
            options.ApiKey = configuration["METALS_API_KEY"] ?? options.ApiKey;
            options.BaseUrl = configuration["METALS_API_BASE_URL"] ?? options.BaseUrl;
            options.Currency = configuration["METALS_API_CURRENCY"] ?? options.Currency;
        });
    }
}
