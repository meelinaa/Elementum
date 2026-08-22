using Elementum.Cli.Hosting;

namespace Elementum.Cli;

/// <summary>
/// Entry point for the Elementum CLI. Applies console configuration and starts the menu host.
/// </summary>
public class Program
{
    /// <summary>Application entry: configure terminal, then run the main input loop until exit.</summary>
    public static void Main()
    {
        CliConsoleConfiguration.Apply();
        using var services = CliServiceCollectionExtensions.CreateServiceProvider();
        CliApplicationHost.Run(services);
    }
}
