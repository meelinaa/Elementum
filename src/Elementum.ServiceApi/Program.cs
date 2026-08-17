using Elementum_ServiceApi.Hosting;
using Serilog;

// Minimal bootstrap logger so failures before host build are visible on the console.
SerilogBootstrap.InitializeGlobalLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Structured logging: sinks and levels from appsettings (Serilog section).
    builder.Host.UseElementumSerilog();

    builder.Services.AddElementumApiServices(builder.Configuration);

    var app = builder.Build();

    app.UseElementumApiPipeline();

    Log.Information("Elementum-ServiceApi started");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
