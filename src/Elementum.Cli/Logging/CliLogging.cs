using Microsoft.Extensions.Logging;

namespace Elementum.Cli.Logging;

/// <summary>Static logger for the CLI. Use for catch blocks and diagnostics.</summary>
public static class CliLogging
{
    private static readonly Lazy<ILoggerFactory> _factory = new Lazy<ILoggerFactory>(() =>
        LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        }));

    /// <summary>Returns a logger for the given category (e.g. type name).</summary>
    public static ILogger GetLogger(string category) => _factory.Value.CreateLogger(category);
}
