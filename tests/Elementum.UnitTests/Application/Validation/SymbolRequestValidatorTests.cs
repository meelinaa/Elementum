using Elementum.Application.Requests;
using Elementum.Application.Validation;

namespace Elementum.Application.Tests.Validation;

public class SymbolRequestValidatorTests
{
    private readonly SymbolRequestValidator _validator = new();

    [Theory]
    [InlineData("XAU")]
    [InlineData("XAG")]
    [InlineData("XPT")]
    [InlineData("XPD")]
    [InlineData("GOLD")]
    [InlineData("SILVER")]
    [InlineData("FOREXCOM:XAUUSD")]
    public void Validate_ValidSymbols_PassesValidation(string symbol)
    {
        var request = new SymbolRequest(symbol);
        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")] // less than 2 chars
    [InlineData("TOOLONGSYMBOLNAMETHATEXCEEDS15CHARS")]
    [InlineData("XAU$USD")] // invalid special char
    public void Validate_InvalidSymbols_FailsValidation(string symbol)
    {
        var request = new SymbolRequest(symbol);
        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
}
