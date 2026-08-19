using Elementum.Domain.Exceptions;
using Elementum.Domain.ValueObjects;

namespace Elementum.UnitTests.Domain.ValueObjects;

public class MoneyTests
{
    // [R]IGHT-BICEP: Verifies that supported currency codes are correctly parsed into Currency instances
    [Fact]
    public void Currency_FromCode_ParsesSupportedCurrencies()
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

    // [B]OUNDARY: Verifies that empty, whitespace, or null currency codes throw DomainValidationException
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Currency_FromCode_EmptyOrNullCode_ThrowsDomainValidationException(string? invalidCode)
    {
        // Arrange, Act & Assert
        Assert.Throws<DomainValidationException>(() => Currency.FromCode(invalidCode!));
    }

    // [E]RROR: Verifies that unsupported or invalid ISO currency codes throw UnsupportedCurrencyException
    [Theory]
    [InlineData("CHF")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("123")]
    public void Currency_FromCode_UnsupportedCode_ThrowsUnsupportedCurrencyException(string invalidCode)
    {
        // Arrange, Act & Assert
        Assert.Throws<UnsupportedCurrencyException>(() => Currency.FromCode(invalidCode));
    }

    // [R]IGHT-BICEP: Verifies that adding two Money instances of the same currency computes the correct sum
    [Fact]
    public void Money_Addition_SameCurrency_ComputesCorrectSum()
    {
        // Arrange
        var m1 = Money.Usd(100.50m);
        var m2 = Money.Usd(50.25m);

        // Act
        var result = m1 + m2;

        // Assert
        Assert.Equal(150.75m, result.Amount);
        Assert.Equal(Currency.USD, result.Currency);
    }

    // [E]RROR: Verifies that adding two Money instances with mismatched currencies throws CurrencyMismatchException
    [Fact]
    public void Money_Addition_DifferentCurrencies_ThrowsCurrencyMismatchException()
    {
        // Arrange
        var m1 = Money.Usd(100m);
        var m2 = Money.Eur(100m);

        // Act & Assert
        Assert.Throws<CurrencyMismatchException>(() => _ = m1 + m2);
    }

    // [I]NVERSE: Verifies that adding and subsequently subtracting an amount returns the exact original value
    [Theory]
    [InlineData(100.50, 45.25)]
    [InlineData(1000.00, 999.99)]
    [InlineData(0.01, 0.02)]
    public void Money_AdditionAndSubtraction_InverseOperationRestoresOriginalAmount(decimal initial, decimal delta)
    {
        // Arrange
        var original = Money.Usd(initial);
        var diff = Money.Usd(delta);

        // Act
        var sum = original + diff;
        var restored = sum - diff;

        // Assert
        Assert.Equal(original.Amount, restored.Amount);
        Assert.Equal(original.Currency, restored.Currency);
    }

    // [C]ROSS-CHECK: Verifies Banker's Rounding (MidpointRounding.ToEven) against explicit rounding logic
    [Fact]
    public void Money_BankersRounding_MatchesMidpointRoundingToEven()
    {
        // Arrange
        var original = Money.Usd(10.55555m);
        decimal expected = Math.Round(10.55555m, 4, MidpointRounding.ToEven);

        // Act
        var rounded = original.Round(4);

        // Assert
        Assert.Equal(expected, rounded.Amount);
        Assert.Equal(10.5556m, rounded.Amount);
    }

    // [B]OUNDARY: Verifies that zero-value Money operations are handled accurately
    [Fact]
    public void Money_ZeroAmountAddition_LeavesAmountUnchanged()
    {
        // Arrange
        var money = Money.Eur(250.75m);
        var zero = Money.Eur(0m);

        // Act
        var result = money + zero;

        // Assert
        Assert.Equal(250.75m, result.Amount);
        Assert.Equal(Currency.EUR, result.Currency);
    }
}
