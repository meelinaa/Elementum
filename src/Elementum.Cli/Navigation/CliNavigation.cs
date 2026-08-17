using Elementum_Cli.Api;
using Elementum_Cli.Enums;
using Elementum_Cli.Views;
using static Elementum_Cli.Output.CliOutputHelper;

namespace Elementum_Cli.Navigation;

/// <summary>
/// Handles keyboard input for the CLI: menu navigation (↑/↓/Enter/ESC) and delegation to the current detail view. [R] clears cache and re-renders.
/// </summary>
public class CliNavigation
{
    /// <summary>Reads one key and either updates menu selection, opens a page, exits, or forwards to the current view (including [R] reload).</summary>
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
            if (key.Key == ConsoleKey.R)
            {
                HttpCall.ClearCache();
                _ = view.RenderAsync();
                return;
            }
            view.HandleInput(key);
        }
    }

    /// <summary>Moves the menu selection up (wraps to bottom if at top); skips non-selectable items.</summary>
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

    /// <summary>Moves the menu selection down (wraps to top if at bottom); skips non-selectable items.</summary>
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

    /// <summary>Ensures SelectedIndex points to a selectable menu item (e.g. after startup).</summary>
    public static void EnsureValidStartIndex()
    {
        var app = AppContext.Current!;
        while (!app.MenuItems[app.SelectedIndex].Selectable)
        {
            app.SelectedIndex++;
        }
    }
}
