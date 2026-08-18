using Elementum.Cli.Navigation;
using Elementum.Cli.Rendering;

namespace Elementum.Cli.Hosting;

/// <summary>
/// Runs the interactive menu loop: initializes <see cref="AppContext"/>, draws the main menu, and delegates keys to <see cref="CliNavigation"/>.
/// </summary>
public static class CliApplicationHost
{
    /// <summary>Builds application state and blocks until the user exits.</summary>
    public static void Run()
    {
        var app = new AppContext();
        app.InitializeMenuItems();
        AppContext.Current = app;

        CliNavigation.EnsureValidStartIndex();
        MenuRenderer.RenderMenu();

        while (app.Running)
            CliNavigation.HandleInput();
    }
}
