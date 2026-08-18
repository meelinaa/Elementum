using Elementum.Cli.Enums;

namespace Elementum.Cli.Constants;

/// <summary>Central place for CLI UI strings.</summary>
public static class CliStrings
{
    private static string MenuLine(string title, string description) =>
        title.PadRight(CliConstants.MenuMainTitleColumnWidth) + " - " + description;

    /// <summary>Left segment of main menu lines; same labels as <see cref="MenuLine"/> titles.</summary>
    private static string MenuTitlePrefix(DetailView view) => view switch
    {
        DetailView.Dashboard => "DASHBOARD",
        DetailView.TradingView => "TRADING",
        DetailView.History => "HISTORY",
        DetailView.Info => "INFO",
        _ => ""
    };

    // --- Footer & navigation ---
    public const string LastUpdateFromPrefix = "Last update from:";
    public const string FooterBackToMenu = "[ESC/←] Back to menu";
    public const string FooterReloadHint = "[R] Reload data";
    public const string PressToSelectEscBack = "Press 1–4 to select. [ESC/←] Back to menu.";
    public const string PressToSelectHistoryEscBack = "Press 1–4 to select. [ESC] Back to metal selection.";

    // --- Main menu ---
    public const string MenuTitle = "ELEMENTUM - METALS DASHBOARD";
    public const string MenuNavigateHint = "[↑/↓] Navigate | [Enter/→] Select | [ESC] Exit";
    public const string MenuFooterHint = "Arrow keys + Enter to navigate";
    public static string MenuItemDashboard => MenuLine(MenuTitlePrefix(DetailView.Dashboard), "Market overview: all metals, price & % change");
    public static string MenuItemTrading => MenuLine(MenuTitlePrefix(DetailView.TradingView), "High/low, open, change, comparison & volatility");
    public static string MenuItemHistory => MenuLine(MenuTitlePrefix(DetailView.History), "Sparkline (Chp) & price bars by period");
    public static string MenuItemInfo => MenuLine(MenuTitlePrefix(DetailView.Info), "API info, daemon schedule & live quotes");
    public static string MenuItemExit => MenuLine("EXIT", "Close the application");

    // --- Metal selection ---
    public const string MetalSelectionHeader = "SELECT METAL";

    /// <summary>Header for the metal picker: <c>SELECT METAL - TRADING</c>.</summary>
    public static string GetMetalSelectionHeaderTitle(DetailView? detailView)
    {
        if (detailView is null)
            return MetalSelectionHeader;
        if (detailView is not (DetailView.TradingView or DetailView.History))
            return MetalSelectionHeader;
        return MetalSelectionHeader + " - " + MenuTitlePrefix(detailView.Value);
    }
    public const string MetalSelectionQuestion = "Which metal should data be displayed for?";
    public const string MetalSelectionOptions = "    [1] Gold      [2] Silver    [3] Platinum  [4] Palladium";

    // --- History view ---
    public const string HistoryHeaderPrefix = "HISTORY — ";
    public const string HistorySelectViewTitle = "Select view";
    public const string HistoryOptionDaily = "  │  [1] Daily       — last 30 days (1 entry per day)                │";
    public const string HistoryOptionWeekly = "  │  [2] Weekly      — last 52 weeks (1 entry per week)              │";
    public const string HistoryOptionMonthly = "  │  [3] Monthly     — last 24 months (1 entry per month)            │";
    public const string HistoryOptionYearly = "  │  [4] Yearly      — Jan & Mid-Year per year                       │";
    public const string HistoryPeriodBoxBottom = "  └──────────────────────────────────────────────────────────────────┘";
    public const string HistoryNoDataMessage = "No history data available for this metal.";
    public const string HistoryNoDataTip = "Tip: Start the daemon worker to collect hourly prices and daily candles.";
    public const string HistorySparklineSectionTitle = "Sparkline — {0} (Chp)";
    public const string HistoryPriceDevelopmentSectionTitle = "Price development ({0})";
    public const string HistoryEntriesSummary = "  Entries: {0} ({1})  ·  From {2:dd.MM.yyyy} to {3:dd.MM.yyyy}";

    // --- Info view ---
    public const string InfoHeader = "INFO — ELEMENTUM TERMINAL";
    public const string InfoSectionTitle = "System Architecture & API Information";
    public const string InfoAppLine = "  │  ELEMENTUM TERMINAL  v2.0 (Clean Architecture & Hexagonal)    │";
    public const string InfoDescriptionLine = "  │  Precious metals spot prices & technical analysis daemon.    │";
    public const string InfoBoxBottom = "  └──────────────────────────────────────────────────────────────┘";
    public const string InfoGoldapiDailyLine = "  │  Live quotes (5 min cache) & hourly daemon from api.edelmetalle.de. │";
    public const string InfoGoldapiQuotaLine = "  │  Daily candles rolled up at 22:00 UTC with 7-day retention.        │";

    // --- Dashboard ---
    public const string DashboardTitle = "CURRENT MARKET OVERVIEW";
}
