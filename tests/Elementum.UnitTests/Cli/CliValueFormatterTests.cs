using System.Globalization;
using Elementum.Cli.Formatting;

namespace Elementum.UnitTests.Cli;

public class CliValueFormatterTests
{
    // [B]OUNDARY: Verifies that null currency inputs return standard placeholder dash
    [Fact]
    public void FormatCurrency_WithNull_ReturnsDash()
    {
        // Arrange & Act & Assert
        Assert.Equal("—", CliValueFormatter.FormatCurrency(null));
        Assert.Equal("—", CliValueFormatter.FormatCurrency(null, "€"));
    }

    // [R]IGHT-BICEP: Verifies that decimal numbers are formatted with accurate decimals and currency symbols
    [Fact]
    public void FormatCurrency_WithValue_ReturnsFormattedString()
    {
        // Arrange
        decimal val = 2500.50m;
        var expectedUsd = val.ToString("N2") + " $";
        var expectedEur = val.ToString("N2") + " €";

        // Act & Assert
        Assert.Equal(expectedUsd, CliValueFormatter.FormatCurrency(val));
        Assert.Equal(expectedEur, CliValueFormatter.FormatCurrency(val, "€"));
    }

    // [B]OUNDARY: Verifies that null percentage inputs return standard placeholder dash
    [Fact]
    public void FormatPercent_WithNull_ReturnsDash()
    {
        // Arrange & Act & Assert
        Assert.Equal("—", CliValueFormatter.FormatPercent(null));
    }

    // [R]IGHT-BICEP: Verifies that positive percentage inputs include explicit '+' prefix
    [Fact]
    public void FormatPercent_WithPositiveValue_IncludesPlusSign()
    {
        // Arrange
        decimal pos = 2.45m;
        decimal zero = 0m;

        // Act & Assert
        Assert.Equal("+" + pos.ToString("0.##") + " %", CliValueFormatter.FormatPercent(pos));
        Assert.Equal("+" + zero.ToString("0.##") + " %", CliValueFormatter.FormatPercent(zero));
    }

    // [R]IGHT-BICEP: Verifies that negative percentage inputs include '-' prefix
    [Fact]
    public void FormatPercent_WithNegativeValue_IncludesMinusSign()
    {
        // Arrange
        decimal neg = -1.75m;

        // Act & Assert
        Assert.Equal(neg.ToString("0.##") + " %", CliValueFormatter.FormatPercent(neg));
    }

    // [B]OUNDARY: Verifies that null values for signed currency format return standard placeholder dash
    [Fact]
    public void FormatCurrencyWithSign_WithNull_ReturnsDash()
    {
        // Arrange & Act & Assert
        Assert.Equal("—", CliValueFormatter.FormatCurrencyWithSign(null));
    }

    // [R]IGHT-BICEP: Verifies that positive amounts formatted with sign include '+' prefix
    [Fact]
    public void FormatCurrencyWithSign_WithPositiveValue_IncludesPlusSign()
    {
        // Arrange
        decimal pos = 150.00m;

        // Act & Assert
        Assert.Equal("+" + pos.ToString("N2") + " $", CliValueFormatter.FormatCurrencyWithSign(pos));
    }

    // [R]IGHT-BICEP: Verifies that negative amounts formatted with sign include '-' prefix
    [Fact]
    public void FormatCurrencyWithSign_WithNegativeValue_IncludesMinusSign()
    {
        // Arrange
        decimal neg = -50.25m;

        // Act & Assert
        Assert.Equal(neg.ToString("N2") + " $", CliValueFormatter.FormatCurrencyWithSign(neg));
    }
}
