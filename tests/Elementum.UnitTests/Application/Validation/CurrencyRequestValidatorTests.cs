using Elementum.Application.Requests;
using Elementum.Application.Validation;

namespace Elementum.Application.Tests.Validation;

public class CurrencyRequestValidatorTests
{
    private readonly CurrencyRequestValidator _validator = new();

    // [R]IGHT-BICEP: omitted or blank currency is allowed so history can return all codes
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_WhenCurrencyMissing_PassesValidation(string? currency)
    {
        var result = _validator.Validate(new CurrencyRequest { Currency = currency });

        Assert.True(result.IsValid);
    }

    // [R]IGHT-BICEP: three-letter codes pass the syntactic filter (supported set is domain)
    [Theory]
    [InlineData("USD")]
    [InlineData("eur")]
    [InlineData("GBP")]
    public void Validate_WhenThreeLetterCode_PassesValidation(string currency)
    {
        var result = _validator.Validate(new CurrencyRequest { Currency = currency });

        Assert.True(result.IsValid);
    }

    // [E]RROR RIGHT-BICEP: non-ISO shapes fail at the HTTP boundary before the domain
    [Theory]
    [InlineData("US")]
    [InlineData("EURO")]
    [InlineData("US$")]
    [InlineData("123")]
    public void Validate_WhenNotThreeLetters_FailsValidation(string currency)
    {
        var result = _validator.Validate(new CurrencyRequest { Currency = currency });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CurrencyRequest.Currency));
    }
}
