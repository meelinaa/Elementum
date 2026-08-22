using Elementum.Domain.Exceptions;
using Elementum.Domain.ValueObjects;

namespace Elementum.UnitTests.Domain.ValueObjects;

public class CurrencyTests
{
    // [R]IGHT-BICEP: Verifies that supported currency codes are correctly parsed into Currency instances
    [Fact]
    public void FromCode_ParsesSupportedCurrencies()
    {
        // Arrange & Act
        var usd = Currency.FromCode("usd");
        var eur = Currency.FromCode("EUR");

        // Assert
        Assert.Equal("USD", usd.Code);
        Assert.Equal(2, usd.DecimalPlaces);
        Assert.Equal("$", usd.Symbol);

        Assert.Equal("EUR", eur.Code);
        Assert.Equal(2, eur.DecimalPlaces);
        Assert.Equal("€", eur.Symbol);
    }

    // RIGHT-[B]ICEP: Verifies that empty, whitespace, or null currency codes throw DomainValidationException
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FromCode_EmptyOrNullCode_ThrowsDomainValidationException(string? invalidCode)
    {
        // Arrange, Act & Assert
        Assert.Throws<DomainValidationException>(() => Currency.FromCode(invalidCode!));
    }

    // RIGHT-BIC[E]P: Verifies that unsupported or invalid ISO currency codes throw UnsupportedCurrencyException
    [Theory]
    [InlineData("CHF")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("123")]
    public void FromCode_UnsupportedCode_ThrowsUnsupportedCurrencyException(string invalidCode)
    {
        // Arrange, Act & Assert
        Assert.Throws<UnsupportedCurrencyException>(() => Currency.FromCode(invalidCode));
    }
}
