namespace Elementum.Shared.Helpers;

/// <summary>
/// Writes to both ILogger and (in Development) to the console with a single call.
/// Use this instead of calling logger and Console separately.
/// </summary>
public class OutputHelper(ILogger<OutputHelper> logger, IHostEnvironment env)
{
    private readonly bool _writeToConsole = env.IsDevelopment();

    /// <summary>Logs and optionally writes to console (in Development).</summary>
    public void Info(string message)
    {
        logger.LogInformation("{Message}", message);
        if (_writeToConsole)
            Console.WriteLine($"[Info] {message}");
    }

    /// <summary>Logs and optionally writes to console (in Development).</summary>
    public void Warning(string message)
    {
        logger.LogWarning("{Message}", message);
        if (_writeToConsole)
            Console.WriteLine($"[Warning] {message}");
    }

    /// <summary>Logs and optionally writes to console (in Development).</summary>
    public void Error(string message, Exception? ex = null)
    {
        if (ex is null)
            logger.LogError("{Message}", message);
        else
            logger.LogError(ex, "{Message}", message);
        if (_writeToConsole)
            Console.WriteLine($"[Error] {message}");
    }

    /// <summary>Convenience alias for Info().</summary>
    public void WriteLine(string message) => Info(message);
}
