using Elementum.Cli.Api;
using Elementum.Cli.Constants;
using Elementum.Cli.Enums;
using Elementum.Cli.Rendering;
using Elementum.Cli.Views;

namespace Elementum.Cli.Navigation;

/// <summary>
/// Handles keyboard input and view navigation for the CLI: menu navigation (↑/↓/Enter/ESC)
/// and delegation to the active detail view.
/// </summary>
public static class CliNavigation
{
    /// <summary>Reads one key and either updates menu selection, opens a page, exits, or forwards to the current view.</summary>
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
                MenuRenderer.DrawMenuItem(previousIndex, false);
                MenuRenderer.DrawMenuItem(app.SelectedIndex, true);
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
            if (key.Key == ConsoleKey.C)
            {
                app.ToggleCurrency();
                HttpCall.ClearCache();
                _ = view.RenderAsync();
                return;
            }
            view.HandleInput(key);
        }
    }

    /// <summary>Opens the currently selected menu item (updates app state and triggers async render).</summary>
    public static void OpenSelectedPage()
    {
        var app = AppContext.Current!;
        var item = app.MenuItems[app.SelectedIndex];
        if (item.View == null || item.Text == CliStrings.MenuItemExit)
        {
            app.Running = false;
            return;
        }

        app.State = AppState.Detail;
        app.CurrentDetailView = item.View;
        if (ViewRegistry.RequiresMetalSelection(item.View!.Value))
            app.CurrentSelectedMetal = null;
        var view = ViewRegistry.Get(item.View.Value);
        _ = view.RenderAsync();
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
