using Elementum.Cli.Logging;
using Microsoft.Extensions.Logging;

namespace Elementum.Cli.Output;

/// <summary>
/// Runs an async action (e.g. API load + render) without a loading spinner. Logs and shows a generic error on failure.
/// </summary>
public class ConsoleLoader
{
    /// <summary>Runs the action without showing a loading spinner. On failure, logs and shows error.</summary>
    public static async Task RunAsync(Func<Task> action, string message = "Loading")
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            CliLogging.GetLogger(nameof(ConsoleLoader)).LogWarning(ex, "Load operation failed");
            CliOutputHelper.ShowError(CliOutputHelper.GenericErrorMessage);
        }
    }
}
