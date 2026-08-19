using Elementum.Cli.Logging;

namespace Elementum.Cli.Output;

/// <summary>
/// Runs an async action (e.g. API load + render) without a loading spinner. Logs and shows a generic error on failure.
/// Uses <see cref="CliLogMessages"/> for zero-allocation structured logging.
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
            CliLogMessages.LoadOperationFailed(CliLogging.GetLogger(nameof(ConsoleLoader)), ex);
            CliOutputHelper.ShowError(CliOutputHelper.GenericErrorMessage);
        }
    }
}
