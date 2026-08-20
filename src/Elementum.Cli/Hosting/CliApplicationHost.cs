using Elementum.Cli.Api;
using Elementum.Cli.Navigation;
using Elementum.Cli.Rendering;
using Elementum.Cli.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Elementum.Cli.Hosting;

/// <summary>
/// Runs the interactive menu loop: initializes <see cref="AppContext"/>, draws the main menu, and delegates keys to <see cref="CliNavigation"/>.
/// </summary>
public static class CliApplicationHost
{
    /// <summary>Builds application state from DI and blocks until the user exits.</summary>
    public static void Run(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var app = new AppContext
        {
            Api = services.GetRequiredService<IHttpCall>(),
            Views = services.GetRequiredService<ViewRegistry>()
        };
        app.InitializeMenuItems();
        AppContext.Current = app;

        CliNavigation.EnsureValidStartIndex();
        MenuRenderer.RenderMenu();

        while (app.Running)
            CliNavigation.HandleInput();
    }
}
