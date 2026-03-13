using Elementum_Cli.Logging;
using Microsoft.Extensions.Logging;

namespace Elementum_Cli.Output;

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
