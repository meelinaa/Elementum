using System.Net;
using Elementum.Infrastructure.Data;
using Elementum.WorkerService.Abstractions;
using Elementum.WorkerService.Jobs;
using Elementum.WorkerService.Observability;
using Elementum.WorkerService.HealthChecks;
using Elementum.WorkerService.Options;
using Elementum.WorkerService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Elementum.WorkerService.Hosting;

/// <summary>
/// Configures the Worker as a minimal ASP.NET Core host (HTTP only for health), Serilog, GoldAPI HttpClient, DI, and background ingestion.
/// </summary>
public static class WorkerHostBuilderExtensions
{
    /// <summary>
    /// Binds Kestrel URLs (defaults to port 5094 to avoid clashing with the main API on 5000). Override with <c>Urls</c> in appsettings or <c>ASPNETCORE_URLS</c>.
    /// </summary>
    public static void ConfigureWorkerListening(this WebApplicationBuilder builder)
    {
        var workerUrls = builder.Configuration["Urls"] ?? "http://localhost:5094";
        builder.WebHost.UseUrls(workerUrls);
    }

    /// <summary>
    /// Replaces default logging with Serilog, reading from configuration (same pattern as the API).
    /// </summary>
    public static void ConfigureWorkerSerilog(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateBootstrapLogger();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);
    }

    /// <summary>
    /// Registers EF Core, Polly-backed HttpClient for GoldAPI, health checks, ingestion job, hosted <see cref="Worker"/>, and metals API options.
    /// </summary>
    public static void AddWorkerApplicationServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING in appsettings.json or .env.");

        services.AddElementumDbContext(connectionString);

        services.AddHttpClient("GoldApi", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MetalsApiOptions>>().Value;
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(options.BaseAddress)
                ? "https://www.goldapi.io/"
                : options.BaseAddress.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(options.ApiKey))
                client.DefaultRequestHeaders.Add("x-access-token", options.ApiKey);
        })
        .AddPolicyHandler(BuildGoldApiRetryPolicy());

        services.AddHealthChecks()
            .AddDbContextCheck<ElementumDbContext>("database", failureStatus: HealthStatus.Unhealthy, tags: new[] { "ready" })
            .AddCheck<GoldApiHealthCheck>("goldapi", failureStatus: HealthStatus.Degraded, tags: new[] { "ready" });

        services.AddSingleton<IngestionMetrics>();
        services.AddSingleton<IMetalsApiClient, MetalsApiClient>();
        services.AddSingleton<IDatabaseCheckService, DatabaseCheckService>();
        services.AddSingleton<IPriceHistoryRepository, PriceHistoryRepository>();
        services.AddScoped<MetalsIngestionJob>();

        services.AddHostedService<global::Elementum.WorkerService.Worker>();

        builder.Host.UseWindowsService();

        ConfigureMetalsApiOptions(services, configuration);
        services.Configure<WorkerScheduleOptions>(configuration.GetSection(WorkerScheduleOptions.SectionName));
    }

    /// <summary>
    /// Binds <see cref="MetalsApiOptions"/> from the MetalsApi section and overrides with common environment variable names.
    /// </summary>
    private static void ConfigureMetalsApiOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MetalsApiOptions>(options =>
        {
            var section = configuration.GetSection(MetalsApiOptions.SectionName);
            section.Bind(options);
            options.ApiKey = configuration["METALS_API_KEY"] ?? options.ApiKey;
            options.BaseUrl = string.IsNullOrWhiteSpace(configuration["METALS_API_BASE_URL"])
                ? options.BaseUrl
                : configuration["METALS_API_BASE_URL"]!.Trim();
            options.StatusUrl = configuration["METALS_API_STATUS"] ?? options.StatusUrl;
            options.RequestStatsUrl = configuration["METALS_API_REQUEST_STATS"] ?? options.RequestStatsUrl;
        });
    }

    /// <summary>
    /// Retries transient HTTP failures and HTTP 429 (rate limit) with exponential backoff (Polly).
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> BuildGoldApiRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
