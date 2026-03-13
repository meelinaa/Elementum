using Elementum_Cli.Enums;
using Elementum_Cli.Constants;
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
            new(CliStrings.MenuItemDashboard, true, 'D', DetailView.Dashboard),
            new("", false),
            new(CliStrings.MenuItemTrading, true, 'T', DetailView.TradingView),
            new(CliStrings.MenuItemKarat, true, 'K', DetailView.KaratCalculator),
            new("", false),
            new(CliStrings.MenuItemListMetals, true, 'L', DetailView.ListMetals),
            new(CliStrings.MenuItemHistory, true, 'H', DetailView.History),
            new("", false),
            new(CliStrings.MenuItemInfo, true, 'I', DetailView.Info),
            new(CliStrings.MenuItemExit, true, 'X', null)
        });
    }
}
