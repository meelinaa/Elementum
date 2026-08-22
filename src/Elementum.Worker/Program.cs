using Elementum.Infrastructure.Outbound.Data;
using Elementum.Worker.Hosting;
using Elementum.Worker.Jobs;
using Elementum.Worker.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

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

    // Liveness for Compose/orchestrators (process-up). Full checks stay on /health.
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
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
