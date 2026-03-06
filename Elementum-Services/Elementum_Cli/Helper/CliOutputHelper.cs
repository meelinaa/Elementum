using Elementum_Cli.Enums;
using Elementum_Cli.Views;

namespace Elementum_Cli.Helper;

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

    public static void RenderViewHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine("╔" + new string('═', ViewWidth - 2) + "╗");
        Console.WriteLine("║ " + title.PadRight(ViewWidth - 4) + " ║");
        Console.WriteLine("╠" + new string('═', ViewWidth - 2) + "╣");
    }

    public static void RenderSectionTitle(string title)
    {
        Console.WriteLine();
        Console.WriteLine("  ┌" + new string('─', ViewWidth - 6) + "┐");
        Console.WriteLine("  │ " + title.PadRight(ViewWidth - 8) + " │");
        Console.WriteLine("  ├" + new string('─', ViewWidth - 6) + "┤");
    }

    public static void RenderViewFooter()
    {
        Console.WriteLine();
        Console.WriteLine("╟" + new string('─', ViewWidth - 2) + "╢");
        Console.WriteLine("║ " + CliStrings.FooterBackToMenu.PadRight(ViewWidth - 4) + " ║");
        Console.WriteLine("╚" + new string('═', ViewWidth - 2) + "╝");
        Console.WriteLine();
    }

    public static void RenderMetalSelectionPrompt()
    {
        RenderViewHeader(CliStrings.MetalSelectionHeader);
        Console.WriteLine("  " + CliStrings.MetalSelectionQuestion);
        Console.WriteLine();
        Console.WriteLine(CliStrings.MetalSelectionOptions);
        Console.WriteLine();
        Console.WriteLine("  " + CliStrings.PressToSelectEscBack);
        RenderViewFooter();
    }

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
