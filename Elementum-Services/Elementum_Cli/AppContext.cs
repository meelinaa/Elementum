using Elementum_Cli.Enums;
using Elementum_Cli.Helper;
using Elementum_Cli.Models;

namespace Elementum_Cli;

/// <summary>
/// Holds all application state for the CLI shell. Created once per run and can be passed or accessed via <see cref="Current"/>.
/// Replaces global static state for easier testing and clearer dependencies.
/// </summary>
public class AppContext
{
    /// <summary>Current context for this run. Set in <see cref="Program.Main"/>.</summary>
    public static AppContext? Current { get; set; }

    public AppState State { get; set; } = AppState.Menu;
    public DetailView? CurrentDetailView { get; set; }
    public Metall? CurrentSelectedMetal { get; set; }
    public bool Running { get; set; } = true;
    public int SelectedIndex { get; set; }
    public Dictionary<int, int> RowMap { get; } = new();
    public List<MenuItem> MenuItems { get; } = new();

    /// <summary>Initializes the default menu items. Call once after construction.</summary>
    public void InitializeMenuItems()
    {
        MenuItems.Clear();
        MenuItems.AddRange(new List<MenuItem>
        {
            new MenuItem("DASHBOARD         - All Metals (Price & Change %)", true, 'D', DetailView.Dashboard),
            new MenuItem("", false),
            new MenuItem("TRADING & DAILY ANALYSIS - Bid/Ask, Compare, Volatility", true, 'T', DetailView.TradingView),
            new MenuItem("KARAT & ALLOY - Price per gram + discount vs. 24k", true, 'K', DetailView.KaratCalculator),
            new MenuItem("", false),
            new MenuItem("List of All Metals- (Gold, Silver, Platinum)", true, 'L', DetailView.ListMetals),
            new MenuItem("HISTORY           - Select Entry Date", true, 'H', DetailView.History),
            new MenuItem("", false),
            new MenuItem("INFO              - Core Data & Creation Date", true, 'I', DetailView.Info),
            new MenuItem("[ESC] Exit Program", true, 'X', null)
        });
    }
}
