using Elementum.Application.Validation;
using Elementum.Domain.Models;
using FluentValidation.TestHelper;

namespace Elementum.UnitTests.Application.Validation;

public class EdelmetalleApiResponseValidatorTests
{
    private readonly EdelmetalleApiResponseValidator _validator = new();

    private static EdelmetalleApiResponse CreateValidResponse() => new()
    {
        GoldUsd = 2500.50m,
        GoldEur = 2280.30m,
        SilberUsd = 30.15m,
        SilberEur = 27.50m,
        PlatinUsd = 1000.00m,
        PlatinEur = 910.00m,
        PalladiumUsd = 1050.00m,
        PalladiumEur = 960.00m,
        WechselkursUsdEur = 1.0950m,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    };

    // [R]IGHT-BICEP: Verifies that realistic market quotes pass full schema validation
    [Fact]
    public void Validate_ValidPayload_ShouldNotHaveValidationError()
    {
        // Arrange
        var model = CreateValidResponse();

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // [B]OUNDARY / [E]RROR: Verifies that non-positive or obsolete historical timestamps fail validation
    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(1600000000)] // Too old (before 2023)
    public void Validate_InvalidTimestamp_ShouldHaveValidationError(long invalidTimestamp)
    {
        // Arrange
        var model = CreateValidResponse() with { Timestamp = invalidTimestamp };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    // [E]RROR: Verifies that timestamps in the future trigger validation failure
    [Fact]
    public void Validate_FutureTimestamp_ShouldHaveValidationError()
    {
        // Arrange
        var futureTimestamp = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();
        var model = CreateValidResponse() with { Timestamp = futureTimestamp };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    // [B]OUNDARY / [E]RROR: Verifies that non-positive or implausibly high gold prices fail validation
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(50000)] // Implausibly high
    public void Validate_InvalidGoldPrice_ShouldHaveValidationError(decimal invalidPrice)
    {
        // Arrange
        var model = CreateValidResponse() with { GoldUsd = invalidPrice };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GoldUsd);
    }

    // [B]OUNDARY / [E]RROR: Verifies that anomalous exchange rate quotes trigger validation failure
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.1)] // Implausibly low exchange rate
    [InlineData(5.0)] // Implausibly high exchange rate
    public void Validate_InvalidExchangeRate_ShouldHaveValidationError(decimal invalidRate)
    {
        // Arrange
        var model = CreateValidResponse() with { WechselkursUsdEur = invalidRate };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WechselkursUsdEur);
    }
}
