using Elementum.Application.Requests;
using Elementum.Application.Validation;

namespace Elementum.Application.Tests.Validation;

public class AggregationRequestValidatorTests
{
    private readonly AggregationRequestValidator _validator = new();

    [Theory]
    [InlineData("daily")]
    [InlineData("weekly")]
    [InlineData("monthly")]
    [InlineData("yearly")]
    [InlineData("d")]
    [InlineData(null)]
    public void Validate_ValidInterval_PassesValidation(string? interval)
    {
        var request = new AggregationRequest("2024-01-01", "2024-01-10", interval);
        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidInterval_FailsValidation()
    {
        var request = new AggregationRequest("2024-01-01", "2024-01-10", "invalid_interval");
        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Interval must be one of"));
    }
}
