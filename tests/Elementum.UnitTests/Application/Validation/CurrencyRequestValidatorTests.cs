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
        var result = _validator.Validate(new CurrencyRequest(currency));

        Assert.True(result.IsValid);
    }

    // [R]IGHT-BICEP: only USD and EUR pass inbound FluentValidation (case-insensitive)
    [Theory]
    [InlineData("USD")]
    [InlineData("usd")]
    [InlineData("EUR")]
    [InlineData("eur")]
    public void Validate_WhenCurrencyIsUsdOrEur_PassesValidation(string currency)
    {
        var result = _validator.Validate(new CurrencyRequest(currency));

        Assert.True(result.IsValid);
    }

    // RIGHT-BIC[E]P: unsupported or malformed codes fail at the HTTP boundary before the domain
    [Theory]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("US")]
    [InlineData("EURO")]
    [InlineData("US$")]
    [InlineData("123")]
    public void Validate_WhenCurrencyUnsupported_FailsValidation(string currency)
    {
        var result = _validator.Validate(new CurrencyRequest(currency));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CurrencyRequest.Currency));
    }
}
