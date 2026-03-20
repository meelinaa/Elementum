using Elementum_WorkerService.Hosting;
using Microsoft.AspNetCore.Builder;
using Serilog;

// Optional local secrets file (development); production should use environment or key vault.
WorkerDotNetEnvConfiguration.LoadOptionalEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureWorkerListening();
builder.ConfigureWorkerSerilog();
builder.AddWorkerApplicationServices();

var app = builder.Build();

// Single health endpoint for orchestrators (includes DB + GoldAPI checks registered in DI).
app.MapHealthChecks("/health");

try
{
    Log.Information("Elementum Worker Service starting.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
    Console.WriteLine();
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey(true);
}
finally
{
    Log.CloseAndFlush();
}
