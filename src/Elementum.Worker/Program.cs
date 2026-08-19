using Elementum.Infrastructure.Outbound.Data;
using Elementum.Worker.Hosting;
using Elementum.Worker.Jobs;
using Elementum.Worker.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

// Optional local secrets file (development); production should use environment or key vault.
WorkerDotNetEnvConfiguration.LoadOptionalEnvFile();

var isSingleRun = args.Any(a => a.Equals("--run-once", StringComparison.OrdinalIgnoreCase) ||
                                a.Equals("--once", StringComparison.OrdinalIgnoreCase) ||
                                a.Equals("-1", StringComparison.OrdinalIgnoreCase));

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureWorkerListening();
builder.ConfigureWorkerSerilog();
builder.AddWorkerApplicationServices();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    await app.Services.ApplyMigrationsAndSeedAsync();

    if (isSingleRun)
    {
        WorkerLogMessages.SingleRunStarting(logger);
        using var scope = app.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<MetalsIngestionJob>();
        await job.RunAsync();
        WorkerLogMessages.SingleRunCompleted(logger);
        return 0;
    }

    // Standard Daemon Mode: listen for health checks and execute on schedule
    app.MapHealthChecks("/health");
    WorkerLogMessages.DaemonStarting(logger);
    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    WorkerLogMessages.ApplicationTerminatedUnexpectedly(logger, ex);
    if (!isSingleRun && Environment.UserInteractive && !Console.IsInputRedirected)
    {
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        try { Console.ReadKey(true); } catch { /* Ignore */ }
    }
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
