namespace Elementum_Cli.Constants;

/// <summary>Central place for CLI UI strings. Simplifies changes and future localization.</summary>
public static class CliStrings
{
    // --- Footer & navigation ---
    public const string FooterBackToMenu = "[ESC/←] Back to menu";
    public const string PressToSelectEscBack = "Press 1–4 to select. [ESC/←] Back to menu.";
    public const string PressToSelectHistoryEscBack = "Press 1–4 to select. [ESC] Back to metal selection.";

    // --- Main menu ---
    public const string MenuTitle = "ELEMENTUM - METALS DASHBOARD";
    public const string MenuNavigateHint = "[↑/↓] Navigate | [Enter/→] Select | [ESC] Exit";
    public const string MenuFooterHint = "Arrow keys + Enter to navigate";
    public const string MenuItemDashboard = "DASHBOARD         - All Metals (Price & Change %)";
    public const string MenuItemTrading = "TRADING & DAILY ANALYSIS - Bid/Ask, Compare, Volatility";
    public const string MenuItemKarat = "KARAT & ALLOY - Price per gram + discount vs. 24k";
    public const string MenuItemListMetals = "List of All Metals- (Gold, Silver, Platinum)";
    public const string MenuItemHistory = "HISTORY           - Select Entry Date";
    public const string MenuItemInfo = "INFO              - Core Data & Creation Date";
    public const string MenuItemExit = "[ESC] Exit Program";

    // --- Metal selection ---
    public const string MetalSelectionHeader = "SELECT METAL";
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
    public const string HistoryPriceDevelopmentLabel = "Price development:";
    public const string HistoryEntriesSummary = "  Entries: {0} ({1})  ·  From {2:yyyy-MM-dd} to {3:yyyy-MM-dd}";

    // --- Info view ---
    public const string InfoHeader = "INFO — ELEMENTUM TERMINAL";
    public const string InfoSectionTitle = "Master data & creation date (CreatedAt)";
    public const string InfoAppLine = "  │  ELEMENTUM TERMINAL  v1.1                                    │";
    public const string InfoDescriptionLine = "  │  Master data & creation date – overview.                     │";
    public const string InfoBoxBottom = "  └──────────────────────────────────────────────────────────────┘";
    public const string InfoBackendPlaceholder = "  (Backend logic to follow)";
    public const string InfoDummyOutput = "  Dummy output (no HTTP call)";

    // --- List metals view ---
    public const string ListMetalsHeader = "METAL MASTER DATA — GOLD, SILVER, PLATINUM";

    // --- Dashboard (title only; table headers stay in view) ---
    public const string DashboardTitle = "CURRENT MARKET OVERVIEW";
}
