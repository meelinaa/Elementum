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

    /// <summary>Current UI state (Menu or Detail).</summary>
    public AppState State { get; set; } = AppState.Menu;

    /// <summary>Which detail view is shown when State is Detail.</summary>
    public DetailView? CurrentDetailView { get; set; }

    /// <summary>Selected metal for views that require one (Trading, Karat, History).</summary>
    public Metall? CurrentSelectedMetal { get; set; }

    /// <summary>When false, the main loop exits and the app closes.</summary>
    public bool Running { get; set; } = true;

    /// <summary>Index of the selected menu item.</summary>
    public int SelectedIndex { get; set; }

    /// <summary>Maps menu item index to console row for redrawing the selection.</summary>
    public Dictionary<int, int> RowMap { get; } = new();

    /// <summary>List of menu entries (title, selectable, key, view).</summary>
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
