using Microsoft.Extensions.Logging;

namespace Elementum_Cli.Helper;

/// <summary>Static logger for the CLI. Use for catch blocks and diagnostics.</summary>
public static class CliLogging
{
    private static readonly Lazy<ILoggerFactory> _factory = new Lazy<ILoggerFactory>(() =>
        LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        }));

    public static ILogger GetLogger(string category) => _factory.Value.CreateLogger(category);
}
