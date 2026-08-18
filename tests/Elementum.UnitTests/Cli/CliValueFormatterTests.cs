using System.Globalization;
using Elementum.Cli.Formatting;

namespace Elementum.UnitTests.Cli;

public class CliValueFormatterTests
{
    [Fact]
    public void FormatCurrency_WithNull_ReturnsDash()
    {
        Assert.Equal("—", CliValueFormatter.FormatCurrency(null));
        Assert.Equal("—", CliValueFormatter.FormatCurrency(null, "€"));
    }

    [Fact]
    public void FormatCurrency_WithValue_ReturnsFormattedString()
    {
        var expected = 2500.50m.ToString("N2") + " $";
        Assert.Equal(expected, CliValueFormatter.FormatCurrency(2500.50m));

        var expectedEur = 2500.50m.ToString("N2") + " €";
        Assert.Equal(expectedEur, CliValueFormatter.FormatCurrency(2500.50m, "€"));
    }

    [Fact]
    public void FormatPercent_WithNull_ReturnsDash()
    {
        Assert.Equal("—", CliValueFormatter.FormatPercent(null));
    }

    [Fact]
    public void FormatPercent_WithPositiveValue_IncludesPlusSign()
    {
        var expected = "+" + 2.45m.ToString("0.##") + " %";
        Assert.Equal(expected, CliValueFormatter.FormatPercent(2.45m));

        var expectedZero = "+" + 0m.ToString("0.##") + " %";
        Assert.Equal(expectedZero, CliValueFormatter.FormatPercent(0m));
    }

    [Fact]
    public void FormatPercent_WithNegativeValue_IncludesMinusSign()
    {
        var expected = (-1.75m).ToString("0.##") + " %";
        Assert.Equal(expected, CliValueFormatter.FormatPercent(-1.75m));
    }

    [Fact]
    public void FormatCurrencyWithSign_WithNull_ReturnsDash()
    {
        Assert.Equal("—", CliValueFormatter.FormatCurrencyWithSign(null));
    }

    [Fact]
    public void FormatCurrencyWithSign_WithPositiveValue_IncludesPlusSign()
    {
        var expected = "+" + 150.00m.ToString("N2") + " $";
        Assert.Equal(expected, CliValueFormatter.FormatCurrencyWithSign(150.00m));
    }

    [Fact]
    public void FormatCurrencyWithSign_WithNegativeValue_IncludesMinusSign()
    {
        var expected = (-50.25m).ToString("N2") + " $";
        Assert.Equal(expected, CliValueFormatter.FormatCurrencyWithSign(-50.25m));
    }
}
