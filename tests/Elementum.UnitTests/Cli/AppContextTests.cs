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

    // [R]IGHT-BICEP: ToggleCurrency switches state between EUR and USD
    [Fact]
    public void ToggleCurrency_SwitchesBetweenEurAndUsd()
    {
        // Arrange
        var app = new Elementum.Cli.AppContext { SelectedCurrency = "EUR" };

        // Act
        app.ToggleCurrency();

        // Assert
        Assert.Equal("USD", app.SelectedCurrency);
    }

    // RIGHT-B[I]CEP: toggling currency twice returns to the initial state
    [Fact]
    public void ToggleCurrency_WhenToggledTwice_ReturnsToInitialCurrency()
    {
        // Arrange
        var app = new Elementum.Cli.AppContext { SelectedCurrency = "EUR" };

        // Act
        app.ToggleCurrency();
        app.ToggleCurrency();

        // Assert
        Assert.Equal("EUR", app.SelectedCurrency);
    }

    // [R]IGHT-BICEP: CurrencySymbol returns corresponding currency symbol for EUR and USD
    [Theory]
    [InlineData("EUR", "€")]
    [InlineData("USD", "$")]
    public void CurrencySymbol_ReturnsExpectedSymbol(string currency, string expectedSymbol)
    {
        // Arrange
        var app = new Elementum.Cli.AppContext { SelectedCurrency = currency };

        // Act & Assert
        Assert.Equal(expectedSymbol, app.CurrencySymbol);
    }
}
