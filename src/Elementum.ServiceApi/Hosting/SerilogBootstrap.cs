using Serilog;

namespace Elementum_ServiceApi.Hosting;

/// <summary>
/// Minimal Serilog setup before the generic host and <c>appsettings</c> are fully loaded.
/// Ensures startup failures and configuration issues can still be written to the console.
/// </summary>
public static class SerilogBootstrap
{
    /// <summary>Creates a bootstrap logger that only writes to the console until <see cref="WebApplicationBuilder.Host"/> Serilog is configured.</summary>
    public static LoggerConfiguration CreateBootstrapLoggerConfiguration() =>
        new LoggerConfiguration()
            .WriteTo.Console();

    /// <summary>Builds and assigns the global <see cref="Log.Logger"/> used during host startup.</summary>
    public static void InitializeGlobalLogger()
    {
        Log.Logger = CreateBootstrapLoggerConfiguration().CreateBootstrapLogger();
    }
}
