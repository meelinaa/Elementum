using Elementum_Cli.Hosting;

namespace Elementum_Cli;

/// <summary>
/// Entry point for the Elementum CLI. Applies console configuration and starts the menu host.
/// </summary>
public class Program
{
    /// <summary>Application entry: configure terminal, then run the main input loop until exit.</summary>
    public static void Main()
    {
        CliConsoleConfiguration.Apply();
        CliApplicationHost.Run();
    }
}
