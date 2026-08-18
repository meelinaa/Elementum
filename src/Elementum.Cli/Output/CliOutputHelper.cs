using Elementum.Cli.Constants;
using Elementum.Cli.Enums;
using Elementum.Cli.Views;

namespace Elementum.Cli.Output;

/// <summary>
/// Shared console output: view headers/footers, section titles, tables, menu rendering, formatting (currency, percent, colors).
/// </summary>
public class CliOutputHelper
{
    /// <summary>View width for headers/footer. Use <see cref="CliConstants.ViewWidth"/> for layout.</summary>
    public static int ViewWidth => CliConstants.ViewWidth;

    /// <summary>Message when no daily data is available for the selected metal.</summary>
    public const string NoDataMessageForMetal = "No daily data available for this metal.";

    /// <summary>Message when a metal list or table is empty.</summary>
    public const string NoMetalsFoundMessage = "No metals found.";

    /// <summary>Generic error message shown in the CLI when an operation fails.</summary>
    public const string GenericErrorMessage = "An error occurred. Please try again.";

    /// <summary>Writes an error message in the CLI (e.g. after a failed request).</summary>
    public static void ShowError(string message)
    {
        Console.WriteLine();
        Console.WriteLine("  " + message);
        RenderViewFooter();
    }

    /// <summary>Formats a decimal as currency with symbol, or "—" if null.</summary>
    public static string FormatCurrency(decimal? value, string symbol = "$")
        => value.HasValue ? value.Value.ToString("N2") + " " + symbol : "—";

    /// <summary>Formats a decimal as percentage with optional leading + for non‑negative, or "—" if null.</summary>
    public static string FormatPercent(decimal? value)
        => value.HasValue ? (value.Value >= 0 ? "+" : "") + value.Value.ToString("0.##") + " %" : "—";

    /// <summary>Formats a decimal as currency with leading + for non‑negative (e.g. for differences), or "—" if null.</summary>
    public static string FormatCurrencyWithSign(decimal? value, string symbol = "$")
        => value.HasValue ? (value.Value >= 0 ? "+" : "") + value.Value.ToString("N2") + " " + symbol : "—";

    /// <summary>Writes the value in green (positive/good) or red (negative), then resets color.</summary>
    public static void WriteColoredValue(string value, bool positiveIsGreen)
    {
        Console.ForegroundColor = positiveIsGreen ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write(value);
        Console.ResetColor();
    }

    /// <summary>Draws the view header: double-line box with title (e.g. "HISTORY — GOLD (XAU)").</summary>
    /// <param name="totalWidth">Outer box width in characters; defaults to <see cref="ViewWidth"/>.</param>
    public static void RenderViewHeader(string title, int? totalWidth = null)
    {
        int w = totalWidth ?? ViewWidth;
        Console.WriteLine();
        Console.WriteLine("╔" + new string('═', w - 2) + "╗");
        Console.WriteLine("║ " + title.PadRight(w - 4) + " ║");
        Console.WriteLine("╠" + new string('═', w - 2) + "╣");
    }

    /// <summary>Draws a section title box (single-line, e.g. "Price development (USD)").</summary>
    /// <param name="totalWidth">Outer width aligned with header/footer; defaults to <see cref="ViewWidth"/>.</param>
    public static void RenderSectionTitle(string title, int? totalWidth = null)
    {
        int w = totalWidth ?? ViewWidth;
        Console.WriteLine();
        Console.WriteLine("  ┌" + new string('─', w - 6) + "┐");
        Console.WriteLine("  │ " + title.PadRight(w - 8) + " │");
        Console.WriteLine("  ├" + new string('─', w - 6) + "┤");
    }

    /// <summary>Draws the view footer: optional last-update line (from DTO <c>EntryDate</c>), back-to-menu, and [R] reload hint.</summary>
    /// <param name="lastUpdate">When set, shown as the first footer line (bottom-left aligned inside the box).</param>
    /// <param name="totalWidth">Outer box width; defaults to <see cref="ViewWidth"/>.</param>
    public static void RenderViewFooter(DateOnly? lastUpdate = null, int? totalWidth = null)
    {
        int w = totalWidth ?? ViewWidth;
        Console.WriteLine();
        Console.WriteLine("╟" + new string('─', w - 2) + "╢");
        if (lastUpdate.HasValue)
        {
            var updateLine = CliStrings.LastUpdateFromPrefix + " " + lastUpdate.Value.ToString(CliConstants.DisplayDateFormat);
            Console.WriteLine("║ " + updateLine.PadRight(w - 4) + " ║");
        }
        Console.WriteLine("║ " + CliStrings.FooterBackToMenu.PadRight(w - 4) + " ║");
        Console.WriteLine("║ " + "[C] Toggle Currency (EUR/USD)".PadRight(w - 4) + " ║");
        Console.WriteLine("║ " + CliStrings.FooterReloadHint.PadRight(w - 4) + " ║");
        Console.WriteLine("╚" + new string('═', w - 2) + "╝");
        Console.WriteLine();
    }

    /// <summary>Draws the view footer with precise DateTime timestamp.</summary>
    public static void RenderViewFooter(DateTime? lastUpdate, int? totalWidth = null)
    {
        int w = totalWidth ?? ViewWidth;
        Console.WriteLine();
        Console.WriteLine("╟" + new string('─', w - 2) + "╢");
        if (lastUpdate.HasValue)
        {
            var updateLine = CliStrings.LastUpdateFromPrefix + " " + lastUpdate.Value.ToString(CliConstants.DisplayDateTimeFormat);
            Console.WriteLine("║ " + updateLine.PadRight(w - 4) + " ║");
        }
        Console.WriteLine("║ " + CliStrings.FooterBackToMenu.PadRight(w - 4) + " ║");
        Console.WriteLine("║ " + "[C] Toggle Currency (EUR/USD)".PadRight(w - 4) + " ║");
        Console.WriteLine("║ " + CliStrings.FooterReloadHint.PadRight(w - 4) + " ║");
        Console.WriteLine("╚" + new string('═', w - 2) + "╝");
        Console.WriteLine();
    }

    /// <summary>Renders the metal selection screen (header, question, options 1–3, footer).</summary>
    public static void RenderMetalSelectionPrompt()
    {
        var header = CliStrings.GetMetalSelectionHeaderTitle(AppContext.Current?.CurrentDetailView);
        RenderViewHeader(header);
        Console.WriteLine("  " + CliStrings.MetalSelectionQuestion);
        Console.WriteLine();
        Console.WriteLine(CliStrings.MetalSelectionOptions);
        Console.WriteLine();
        Console.WriteLine("  " + CliStrings.PressToSelectEscBack);
        RenderViewFooter();
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

    /// <summary>Opens the currently selected menu item (sets state, view, and calls RenderAsync). Exits if item is Exit.</summary>
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

    /// <summary>Clears the console and draws the main menu (title, hint, all menu items with selection).</summary>
    public static void RenderMenu()
    {
        var app = AppContext.Current!;
        Console.Clear();
        app.RowMap.Clear();

        //RenderTageswerteHeader();
        Console.WriteLine("=================================================================================");
        Console.WriteLine($"                         ELEMENTUM - METALS DASHBOARD");
        Console.WriteLine("=================================================================================");
        Console.WriteLine();
        Console.WriteLine("                 [↑/↓] Navigate | [Enter/→] Select | [ESC] Exit");
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
}
