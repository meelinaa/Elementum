using Elementum.Shared.Helpers;
using Elementum_WorkerService;
using Elementum_WorkerService.Options;

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

builder.Services.AddSingleton<OutputHelper>();
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
