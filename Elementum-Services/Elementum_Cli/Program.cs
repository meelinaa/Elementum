using Elementum_Cli.Navigation;
using Elementum_Cli.Output;
using System.Text;

namespace Elementum_Cli;

/// <summary>
/// Entry point for the Elementum CLI. Sets up console encoding, creates <see cref="AppContext"/>, and runs the main input loop.
/// </summary>
public class Program
{
    /// <summary>Initializes the CLI, renders the menu, and processes keyboard input until the user exits.</summary>
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;

        var app = new AppContext();
        app.InitializeMenuItems();
        AppContext.Current = app;

        CliNavigation.EnsureValidStartIndex();
        CliOutputHelper.RenderMenu();

        while (app.Running)
        {
            CliNavigation.HandleInput();
        }
    }
}
