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
    [InlineData("CHF")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("123")]
    public void Currency_FromCode_UnsupportedOrInvalidCode_ThrowsArgumentException(string invalidCode)
    {
        Assert.Throws<ArgumentException>(() => Currency.FromCode(invalidCode));
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
    public void Money_Addition_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        var m1 = Money.Usd(100m);
        var m2 = Money.Eur(100m);

        Assert.Throws<InvalidOperationException>(() => _ = m1 + m2);
    }

    [Fact]
    public void Money_RoundToCash_Uses2Decimals()
    {
        var m1 = Money.Eur(2.434m).RoundToCash();
        Assert.Equal(2.43m, m1.Amount);

        var m2 = Money.Usd(2.436m).RoundToCash();
        Assert.Equal(2.44m, m2.Amount);
    }

    [Fact]
    public void Money_BankersRounding_ToEvenDecimals()
    {
        var m = Money.Usd(10.55555m).Round(4);
        Assert.Equal(10.5556m, m.Amount);
    }
}
