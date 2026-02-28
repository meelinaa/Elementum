using Elementum.Infrastructure.Data;
using Elementum_WorkerService;
using Elementum_WorkerService.Abstractions;
using Elementum_WorkerService.Jobs;
using Elementum_WorkerService.Options;
using Elementum_WorkerService.Services;

// Load .env from current or parent directory (optional; in production use real env vars)
try
{
    if (File.Exists(".env"))
        DotNetEnv.Env.Load();
    else
        DotNetEnv.Env.TraversePath().Load();
}
catch (FileNotFoundException) { /* .env optional when using real env vars */ }

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["CONNECTION_STRING"];
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection or CONNECTION_STRING in appsettings.json or .env.");
builder.Services.AddElementumDbContext(connectionString);

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

var host = builder.Build();
host.Run();
