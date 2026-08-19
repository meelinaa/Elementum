using Elementum.Cli.Constants;

namespace Elementum.Cli.Tests;

public class AppContextTests
{
    // [R]IGHT-BICEP: menu initialization registers all expected navigation entries
    [Fact]
    public void InitializeMenuItems_CreatesExpectedMenuEntries()
    {
        // Arrange
        var app = new Elementum.Cli.AppContext();

        // Act
        app.InitializeMenuItems();

        // Assert
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemDashboard);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemTrading);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemHistory);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemInfo);
        Assert.Contains(app.MenuItems, mi => mi.Text == CliStrings.MenuItemExit);
    }
}
