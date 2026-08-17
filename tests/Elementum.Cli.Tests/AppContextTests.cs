using Elementum_Cli.Constants;

namespace Elementum.Cli.Tests;

public class AppContextTests
{
    [Fact]
    public void InitializeMenuItems_CreatesExpectedMenuEntries()
    {
        var app = new Elementum_Cli.AppContext();
        app.InitializeMenuItems();

        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemDashboard);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemTrading);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemKarat);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemListMetals);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemHistory);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemInfo);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemExit);
    }
}

