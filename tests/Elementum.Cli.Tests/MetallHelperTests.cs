using Elementum.Cli.Enums;
using Elementum.Cli.Input;

namespace Elementum.Cli.Tests;

public class MetallHelperTests
{
    [Theory]
    [InlineData(Metall.Gold, "XAU", "Gold")]
    [InlineData(Metall.Silber, "XAG", "Silver")]
    [InlineData(Metall.Platin, "XPT", "Platinum")]
    public void GetSymbolAndName_ReturnExpectedValues(Metall m, string symbol, string name)
    {
        Assert.Equal(symbol, MetallHelper.GetSymbol(m));
        Assert.Equal(name, MetallHelper.GetName(m));
    }

    [Theory]
    [InlineData('1', Metall.Gold)]
    [InlineData('2', Metall.Silber)]
    [InlineData('3', Metall.Platin)]
    public void FromKey_MapsDigits(char c, Metall expected)
    {
        Assert.Equal(expected, MetallHelper.FromKey(c));
    }

    [Fact]
    public void FromKey_ReturnsNullForUnknown()
    {
        Assert.Null(MetallHelper.FromKey('x'));
    }

    [Theory]
    [InlineData(ConsoleKey.D1, Metall.Gold)]
    [InlineData(ConsoleKey.NumPad2, Metall.Silber)]
    [InlineData(ConsoleKey.D3, Metall.Platin)]
    public void FromConsoleKey_MapsKeys(ConsoleKey key, Metall expected)
    {
        Assert.Equal(expected, MetallHelper.FromConsoleKey(key));
    }
}

