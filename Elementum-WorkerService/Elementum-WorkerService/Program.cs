using System.Net;
using Elementum.Infrastructure.Data;
using Elementum_WorkerService;
using Elementum_WorkerService.Abstractions;
using Elementum_WorkerService.Jobs;
using Elementum_WorkerService.Observability;
using Elementum_WorkerService.HealthChecks;
using Elementum_WorkerService.Options;
using Elementum_WorkerService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Microsoft.AspNetCore.Builder;

// Load .env from current or parent directory (optional; in production use real env vars)
try
{
    if (File.Exists(".env"))
        DotNetEnv.Env.Load();
    else
        DotNetEnv.Env.TraversePath().Load();
}
catch (FileNotFoundException) { /* .env optional when using real env vars */ }

var builder = WebApplication.CreateBuilder(args);

// Structured logging with Serilog
builder.Logging.ClearProviders();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["CONNECTION_STRING"];
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING in appsettings.json or .env.");
builder.Services.AddElementumDbContext(connectionString);

// HTTP client with Polly retry on transient failures (3 retries, exponential backoff)
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

builder.Services.AddHttpClient("GoldApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<MetalsApiOptions>>().Value;
    client.BaseAddress = new Uri("https://www.goldapi.io/");
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrEmpty(options.ApiKey))
        client.DefaultRequestHeaders.Add("x-access-token", options.ApiKey);
})
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ElementumDbContext>("database", failureStatus: HealthStatus.Unhealthy, tags: new[] { "ready" })
    .AddCheck<GoldApiHealthCheck>("goldapi", failureStatus: HealthStatus.Degraded, tags: new[] { "ready" });

builder.Services.AddSingleton<IngestionMetrics>();
builder.Services.AddSingleton<IMetalsApiClient, MetalsApiClient>();
builder.Services.AddSingleton<IDatabaseCheckService, DatabaseCheckService>();
builder.Services.AddSingleton<IPriceHistoryRepository, PriceHistoryRepository>();
builder.Services.AddScoped<MetalsIngestionJob>();

builder.Services.AddHostedService<Worker>();

// Bind options from env / appsettings (keys: METALS_API_KEY, METALS_API_BASE_URL, etc.)
builder.Services.Configure<MetalsApiOptions>(options =>
{
    var c = builder.Configuration;
    options.ApiKey = c["METALS_API_KEY"] ?? string.Empty;
    options.BaseUrl = (c["METALS_API_BASE_URL"] ?? string.Empty).Trim();
    options.StatusUrl = c["METALS_API_STATUS"] ?? string.Empty;
    options.RequestStatsUrl = c["METALS_API_REQUEST_STATS"] ?? string.Empty;
});

var app = builder.Build();

app.MapHealthChecks("/health");

Log.Information("Elementum Worker Service starting.");
await app.RunAsync();
Log.CloseAndFlush();
