using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Elementum.Api.Hosting;

/// <summary>
/// Serilog setup and bootstrap for Elementum.Api.
/// </summary>
public static class SerilogBootstrap
{
    public static LoggerConfiguration CreateBootstrapLoggerConfiguration() =>
        new LoggerConfiguration()
            .WriteTo.Console();

    public static void InitializeGlobalLogger()
    {
        Log.Logger = CreateBootstrapLoggerConfiguration().CreateBootstrapLogger();
    }

    public static ConfigureHostBuilder UseElementumSerilog(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());
        return host;
    }
}
