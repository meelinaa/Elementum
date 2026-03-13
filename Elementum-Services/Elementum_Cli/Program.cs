using Elementum_Cli.Helper;
using System.Text;

namespace Elementum_Cli;

public class Program
{
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
