using Elementum.Application.Requests;
using Elementum.Application.Validation;

namespace Elementum.Application.Tests.Validation;

public class SymbolRequestValidatorTests
{
    private readonly SymbolRequestValidator _validator = new();

    // [R]IGHT-BICEP: valid metal symbols pass FluentValidation rules for inbound route binding
    [Theory]
    [InlineData("XAU")]
    [InlineData("XAG")]
    [InlineData("XPT")]
    [InlineData("XPD")]
    [InlineData("GOLD")]
    [InlineData("SILVER")]
    [InlineData("FOREXCOM:XAUUSD")]
    public void Validate_WhenSymbolIsValid_PassesValidation(string symbol)
    {
        // Arrange
        var request = new SymbolRequest(symbol);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // [B]OUNDARY RIGHT-BICEP: symbol at exactly MaxLength(15) is the last acceptable inbound value
    [Fact]
    public void Validate_WhenSymbolIsExactly15Characters_PassesValidation()
    {
        // Arrange
        const string symbolAtMaxLength = "ABCDEFGHIJKLMNO";
        var request = new SymbolRequest(symbolAtMaxLength);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // [E]RROR RIGHT-BICEP: invalid symbols must fail with at least one validation error message
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    [InlineData("TOOLONGSYMBOLNAMETHATEXCEEDS15CHARS")]
    [InlineData("XAU$USD")]
    public void Validate_WhenSymbolIsInvalid_FailsValidation(string symbol)
    {
        // Arrange
        var request = new SymbolRequest(symbol);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
}
