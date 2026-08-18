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

    [Fact]
    public void Validate_ValidPayload_ShouldNotHaveValidationError()
    {
        var model = CreateValidResponse();
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(1600000000)] // Too old (before 2023)
    public void Validate_InvalidTimestamp_ShouldHaveValidationError(long invalidTimestamp)
    {
        var model = CreateValidResponse() with { Timestamp = invalidTimestamp };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    [Fact]
    public void Validate_FutureTimestamp_ShouldHaveValidationError()
    {
        var futureTimestamp = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();
        var model = CreateValidResponse() with { Timestamp = futureTimestamp };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(50000)] // Implausibly high
    public void Validate_InvalidGoldPrice_ShouldHaveValidationError(decimal invalidPrice)
    {
        var model = CreateValidResponse() with { GoldUsd = invalidPrice };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.GoldUsd);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.1)] // Implausibly low exchange rate
    [InlineData(5.0)] // Implausibly high exchange rate
    public void Validate_InvalidExchangeRate_ShouldHaveValidationError(decimal invalidRate)
    {
        var model = CreateValidResponse() with { WechselkursUsdEur = invalidRate };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.WechselkursUsdEur);
    }
}
