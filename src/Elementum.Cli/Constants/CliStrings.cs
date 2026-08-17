using Elementum.Cli.Enums;

namespace Elementum.Cli.Constants;

/// <summary>Central place for CLI UI strings. Simplifies changes and future localization.</summary>
public static class CliStrings
{
    private static string MenuLine(string title, string description) =>
        title.PadRight(CliConstants.MenuMainTitleColumnWidth) + " - " + description;

    /// <summary>Left segment of main menu lines; same labels as <see cref="MenuLine"/> titles.</summary>
    private static string MenuTitlePrefix(DetailView view) => view switch
    {
        DetailView.Dashboard => "DASHBOARD",
        DetailView.ListMetals => "METAL LIST",
        DetailView.TradingView => "TRADING",
        DetailView.KaratCalculator => "KARAT",
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
    public static string MenuItemTrading => MenuLine(MenuTitlePrefix(DetailView.TradingView), "Bid/ask, spreads, volatility & daily comparison");
    public static string MenuItemKarat => MenuLine(MenuTitlePrefix(DetailView.KaratCalculator), "Gram prices by purity, alloy discount vs 24k");
    public static string MenuItemListMetals => MenuLine(MenuTitlePrefix(DetailView.ListMetals), "Master table: id, symbol, name");
    public static string MenuItemHistory => MenuLine(MenuTitlePrefix(DetailView.History), "Sparkline (Chp) & price bars by period");
    public static string MenuItemInfo => MenuLine(MenuTitlePrefix(DetailView.Info), "GoldAPI policy, daily limits, no live data");
    public static string MenuItemExit => MenuLine("EXIT", "Close the application");

    // --- Metal selection ---
    public const string MetalSelectionHeader = "SELECT METAL";

    /// <summary>Header for the metal picker: <c>SELECT METAL - TRADING</c> (uses same title segment as the main menu).</summary>
    public static string GetMetalSelectionHeaderTitle(DetailView? detailView)
    {
        if (detailView is null)
            return MetalSelectionHeader;
        if (detailView is not (DetailView.TradingView or DetailView.KaratCalculator or DetailView.History))
            return MetalSelectionHeader;
        return MetalSelectionHeader + " - " + MenuTitlePrefix(detailView.Value);
    }
    public const string MetalSelectionQuestion = "Which metal should data be displayed for?";
    public const string MetalSelectionOptions = "    [1] Gold        [2] Silver      [3] Platinum ";

    // --- History view ---
    public const string HistoryHeaderPrefix = "HISTORY — ";
    public const string HistorySelectViewTitle = "Select view";
    public const string HistoryOptionDaily = "  │  [1] Daily       — last 30 entries (day by day)                  │";
    public const string HistoryOptionWeekly = "  │  [2] Weekly      — 52 values (per week)                          │";
    public const string HistoryOptionMonthly = "  │  [3] Monthly     — 12 values (per month)                         │";
    public const string HistoryOptionYearly = "  │  [4] Yearly      — 10 values (per year)                          │";
    public const string HistoryPeriodBoxBottom = "  └──────────────────────────────────────────────────────────────────┘";
    public const string HistoryNoDataMessage = "No history data available for this metal.";
    public const string HistoryNoDataTip = "Tip: Implement API endpoint history/{symbol}/aggregated?aggregation=&count=.";
    public const string HistorySparklineSectionTitle = "Sparkline — {0} (Chp)";
    public const string HistoryPriceDevelopmentSectionTitle = "Price development (USD)";
    public const string HistoryEntriesSummary = "  Entries: {0} ({1})  ·  From {2:dd.MM.yyyy} to {3:dd.MM.yyyy}";

    // --- Info view ---
    public const string InfoHeader = "INFO — ELEMENTUM TERMINAL";
    public const string InfoSectionTitle = "Master data & creation date (CreatedAt)";
    public const string InfoAppLine = "  │  ELEMENTUM TERMINAL  v1.1                                    │";
    public const string InfoDescriptionLine = "  │  Master data & creation date – overview.                     │";
    public const string InfoBoxBottom = "  └──────────────────────────────────────────────────────────────┘";
    public const string InfoGoldapiDailyLine = "  │  Market prices are requested once per day from the GoldAPI.com API. │";
    public const string InfoGoldapiQuotaLine = "  │  Monthly request limits apply; this app does not use live data.    │";

    // --- List metals view ---
    public const string ListMetalsHeader = "METAL MASTER DATA — GOLD, SILVER, PLATINUM";

    // --- Dashboard (title only; table headers stay in view) ---
    public const string DashboardTitle = "CURRENT MARKET OVERVIEW";
}
