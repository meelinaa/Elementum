using Elementum.Cli.Constants;

namespace Elementum.Cli.Rendering;

/// <summary>
/// Handles rendering and updating the main CLI navigation menu.
/// </summary>
public static class MenuRenderer
{
    /// <summary>Clears the console and draws the main menu (title, hint, all menu items with selection).</summary>
    public static void RenderMenu()
    {
        var app = AppContext.Current!;
        Console.Clear();
        app.RowMap.Clear();

        Console.WriteLine("=================================================================================");
        int titlePadding = Math.Max(0, (81 - CliStrings.MenuTitle.Length) / 2);
        Console.WriteLine(new string(' ', titlePadding) + CliStrings.MenuTitle);
        Console.WriteLine("=================================================================================");
        Console.WriteLine();
        int hintPadding = Math.Max(0, (81 - CliStrings.MenuNavigateHint.Length) / 2);
        Console.WriteLine(new string(' ', hintPadding) + CliStrings.MenuNavigateHint);
        Console.WriteLine();

        for (int i = 0; i < app.MenuItems.Count; i++)
        {
            if (!app.MenuItems[i].Selectable && string.IsNullOrWhiteSpace(app.MenuItems[i].Text))
            {
                Console.WriteLine();
                continue;
            }

            app.RowMap[i] = Console.CursorTop;
            DrawMenuItem(i, i == app.SelectedIndex);
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------------------");
        Console.WriteLine("                        " + CliStrings.MenuFooterHint + "                             ");
        Console.WriteLine("=================================================================================");
    }

    /// <summary>Redraws a single menu item at the stored row; highlights with arrow when selected.</summary>
    public static void DrawMenuItem(int index, bool selected)
    {
        var app = AppContext.Current!;
        if (!app.RowMap.ContainsKey(index))
            return;

        Console.SetCursorPosition(0, app.RowMap[index]);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, app.RowMap[index]);

        if (!app.MenuItems[index].Selectable)
        {
            Console.Write("   " + app.MenuItems[index].Text);
            return;
        }

        if (selected)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" ▶ " + app.MenuItems[index].Text);
            Console.ResetColor();
        }
        else
        {
            Console.Write("   " + app.MenuItems[index].Text);
        }
    }
}
