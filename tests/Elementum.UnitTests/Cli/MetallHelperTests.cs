using Elementum.Cli.Enums;
using Elementum.Cli.Input;

namespace Elementum.Cli.Tests;

public class MetallHelperTests
{
    // [R]IGHT-BICEP: Verifies that Metall enum values map to ISO metal symbol and display names
    [Theory]
    [InlineData(Metall.Gold, "XAU", "Gold")]
    [InlineData(Metall.Silber, "XAG", "Silver")]
    [InlineData(Metall.Platin, "XPT", "Platinum")]
    public void GetSymbolAndName_ReturnExpectedValues(Metall m, string symbol, string name)
    {
        // Arrange & Act & Assert
        Assert.Equal(symbol, MetallHelper.GetSymbol(m));
        Assert.Equal(name, MetallHelper.GetName(m));
    }

    // [R]IGHT-BICEP: Verifies that digit characters map to appropriate Metall enum members
    [Theory]
    [InlineData('1', Metall.Gold)]
    [InlineData('2', Metall.Silber)]
    [InlineData('3', Metall.Platin)]
    public void FromKey_MapsDigits(char c, Metall expected)
    {
        // Arrange & Act & Assert
        Assert.Equal(expected, MetallHelper.FromKey(c));
    }

    // [B]OUNDARY / [E]RROR: Verifies that unrecognized keyboard characters return null
    [Fact]
    public void FromKey_ReturnsNullForUnknown()
    {
        // Arrange & Act & Assert
        Assert.Null(MetallHelper.FromKey('x'));
    }

    // [R]IGHT-BICEP: Verifies that ConsoleKey codes (number row and numpad) map correctly
    [Theory]
    [InlineData(ConsoleKey.D1, Metall.Gold)]
    [InlineData(ConsoleKey.NumPad2, Metall.Silber)]
    [InlineData(ConsoleKey.D3, Metall.Platin)]
    public void FromConsoleKey_MapsKeys(ConsoleKey key, Metall expected)
    {
        // Arrange & Act & Assert
        Assert.Equal(expected, MetallHelper.FromConsoleKey(key));
    }
}
