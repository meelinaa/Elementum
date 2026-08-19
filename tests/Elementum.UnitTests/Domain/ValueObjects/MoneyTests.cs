using Elementum.Domain.Exceptions;
using Elementum.Domain.ValueObjects;

namespace Elementum.UnitTests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Currency_FromCode_ParsesSupportedCurrencies()
    {
        var usd = Currency.FromCode("usd");
        Assert.Equal("USD", usd.Code);
        Assert.Equal(2, usd.DecimalPlaces);
        Assert.Equal("$", usd.Symbol);

        var eur = Currency.FromCode("EUR");
        Assert.Equal("EUR", eur.Code);
        Assert.Equal(2, eur.DecimalPlaces);
        Assert.Equal("€", eur.Symbol);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Currency_FromCode_EmptyCode_ThrowsDomainValidationException(string? invalidCode)
    {
        Assert.Throws<DomainValidationException>(() => Currency.FromCode(invalidCode!));
    }

    [Theory]
    [InlineData("CHF")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("123")]
    public void Currency_FromCode_UnsupportedCode_ThrowsUnsupportedCurrencyException(string invalidCode)
    {
        Assert.Throws<UnsupportedCurrencyException>(() => Currency.FromCode(invalidCode));
    }

    [Fact]
    public void Money_Addition_SameCurrency_ComputesCorrectSum()
    {
        var m1 = Money.Usd(100.50m);
        var m2 = Money.Usd(50.25m);

        var result = m1 + m2;

        Assert.Equal(150.75m, result.Amount);
        Assert.Equal(Currency.USD, result.Currency);
    }

    [Fact]
    public void Money_Addition_DifferentCurrencies_ThrowsCurrencyMismatchException()
    {
        var m1 = Money.Usd(100m);
        var m2 = Money.Eur(100m);

        Assert.Throws<CurrencyMismatchException>(() => _ = m1 + m2);
    }

    [Fact]
    public void Money_BankersRounding_ToEvenDecimals()
    {
        var m = Money.Usd(10.55555m).Round(4);
        Assert.Equal(10.5556m, m.Amount);
    }
}
