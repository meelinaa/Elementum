using Elementum_Cli.Enums;

namespace Elementum_Cli.Helper;

public class HandleInputHelper
{
    public static void HandleInput(ConsoleKeyInfo key)
    {
        var app = AppContext.Current!;
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
        {
            app.State = AppState.Menu;
            app.CurrentDetailView = null;
            CliOutputHelper.RenderMenu();
        }
    }

    /// <summary>
    /// Handles input for views that require a metal selection (1–3 or D1–D3).
    /// When a metal is selected, <paramref name="renderAsync"/> is invoked to (re-)render the view content.
    /// Use this from your view's HandleInput and pass a method that only performs the output (e.g. () => RenderAsync()).
    /// </summary>
    /// <param name="key">The key pressed.</param>
    /// <param name="renderAsync">Func that renders the current view (e.g. () => RenderAsync()). Called after metal selection; task is fire-and-forget.</param>
    public static void HandleInputWithMetals(ConsoleKeyInfo key, Func<Task>? renderAsync)
    {
        var app = AppContext.Current!;
        if (!app.CurrentSelectedMetal.HasValue)
        {
            var metall = MetallHelper.FromKey(key.KeyChar) ?? MetallHelper.FromConsoleKey(key.Key);
            if (metall.HasValue)
            {
                app.CurrentSelectedMetal = metall;
                _ = renderAsync?.Invoke();
            }
            else if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
            {
                app.State = AppState.Menu;
                app.CurrentDetailView = null;
                CliOutputHelper.RenderMenu();
            }
            return;
        }
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
        {
            app.State = AppState.Menu;
            app.CurrentDetailView = null;
            app.CurrentSelectedMetal = null;
            CliOutputHelper.RenderMenu();
        }
    }
}
