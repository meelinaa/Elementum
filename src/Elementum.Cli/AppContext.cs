using Elementum.Cli.Enums;
using Elementum.Cli.Constants;
using Elementum.Cli.Models;

namespace Elementum.Cli;

/// <summary>
/// Holds all application state for the CLI shell.
/// </summary>
public class AppContext
{
    /// <summary>Current context for this run. Set in <see cref="Program.Main"/>.</summary>
    public static AppContext? Current { get; set; }

    /// <summary>Current UI state (Menu or Detail).</summary>
    public AppState State { get; set; } = AppState.Menu;

    /// <summary>Which detail view is shown when State is Detail.</summary>
    public DetailView? CurrentDetailView { get; set; }

    /// <summary>Selected metal for views that require one (Trading, History).</summary>
    public Metall? CurrentSelectedMetal { get; set; }

    /// <summary>Selected currency for display (EUR or USD).</summary>
    public string SelectedCurrency { get; set; } = "EUR";

    /// <summary>Currency symbol for current selection (€ or $).</summary>
    public string CurrencySymbol => SelectedCurrency == "EUR" ? "€" : "$";

    /// <summary>Toggles between EUR and USD.</summary>
    public void ToggleCurrency() => SelectedCurrency = SelectedCurrency == "EUR" ? "USD" : "EUR";

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
            new(CliStrings.MenuItemHistory, true, 'H', DetailView.History),
            new("", false),
            new(CliStrings.MenuItemInfo, true, 'I', DetailView.Info),
            new(CliStrings.MenuItemExit, true, 'X', null)
        });
    }
}
