namespace Elementum.Cli.Formatting;

/// <summary>
/// Provides string and number formatting for currencies, percentages, and signed values in the CLI.
/// </summary>
public static class CliValueFormatter
{
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
}
