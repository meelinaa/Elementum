using Elementum.Worker.Hosting;
using Elementum.Worker.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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

try
{
    if (isSingleRun)
    {
        Log.Information("Elementum Worker starting in Single-Run mode (--run-once / --once).");
        using var scope = app.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<MetalsIngestionJob>();
        await job.RunAsync();
        Log.Information("Single-Run ingestion completed successfully.");
        return 0;
    }

    // Standard Daemon Mode: listen for health checks and execute on schedule
    app.MapHealthChecks("/health");
    Log.Information("Elementum Worker Service starting in Daemon mode.");
    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
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
