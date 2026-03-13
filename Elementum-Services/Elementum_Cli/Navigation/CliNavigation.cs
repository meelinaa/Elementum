using Elementum_Cli.Enums;
using Elementum_Cli.Views;
using static Elementum_Cli.Output.CliOutputHelper;

namespace Elementum_Cli.Navigation;

public class CliNavigation
{
    public static void HandleInput()
    {
        var app = AppContext.Current!;
        var key = Console.ReadKey(true);

        if (app.State == AppState.Menu)
        {
            int previousIndex = app.SelectedIndex;

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    MoveUp();
                    break;

                case ConsoleKey.DownArrow:
                    MoveDown();
                    break;

                case ConsoleKey.Enter:
                case ConsoleKey.RightArrow:
                    OpenSelectedPage();
                    return;

                case ConsoleKey.Escape:
                    app.Running = false;
                    return;
            }

            if (previousIndex != app.SelectedIndex)
            {
                DrawMenuItem(previousIndex, false);
                DrawMenuItem(app.SelectedIndex, true);
            }
        }
        else if (app.State == AppState.Detail && app.CurrentDetailView.HasValue)
        {
            var view = ViewRegistry.Get(app.CurrentDetailView.Value);
            view.HandleInput(key);
        }
    }

    public static void MoveUp()
    {
        var app = AppContext.Current!;
        do
        {
            app.SelectedIndex--;
            if (app.SelectedIndex < 0)
                app.SelectedIndex = app.MenuItems.Count - 1;
        }
        while (!app.MenuItems[app.SelectedIndex].Selectable);
    }

    public static void MoveDown()
    {
        var app = AppContext.Current!;
        do
        {
            app.SelectedIndex++;
            if (app.SelectedIndex >= app.MenuItems.Count)
                app.SelectedIndex = 0;
        }
        while (!app.MenuItems[app.SelectedIndex].Selectable);
    }

    public static void EnsureValidStartIndex()
    {
        var app = AppContext.Current!;
        while (!app.MenuItems[app.SelectedIndex].Selectable)
        {
            app.SelectedIndex++;
        }
    }
}
