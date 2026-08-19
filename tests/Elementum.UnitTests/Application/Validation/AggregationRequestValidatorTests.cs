using Elementum.Application.Requests;
using Elementum.Application.Validation;

namespace Elementum.Application.Tests.Validation;

public class AggregationRequestValidatorTests
{
    private readonly AggregationRequestValidator _validator = new();

    // [R]IGHT-BICEP: Verifies that supported aggregation intervals pass validation
    [Theory]
    [InlineData("daily")]
    [InlineData("weekly")]
    [InlineData("monthly")]
    [InlineData("yearly")]
    [InlineData("d")]
    [InlineData(null)]
    public void Validate_ValidInterval_PassesValidation(string? interval)
    {
        // Arrange
        var request = new AggregationRequest("2024-01-01", "2024-01-10", interval);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // [E]RROR RIGHT-BICEP: unsupported or arbitrary interval strings fail validation
    [Fact]
    public void Validate_InvalidInterval_FailsValidation()
    {
        // Arrange
        var request = new AggregationRequest("2024-01-01", "2024-01-10", "invalid_interval");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Interval must be one of"));
    }

    // [E]RROR RIGHT-BICEP: non-ISO date strings fail validation on FirstDate
    [Fact]
    public void Validate_WhenFirstDateFormatInvalid_FailsValidation()
    {
        // Arrange
        var request = new AggregationRequest("01/31/2024", "2024-01-10", "daily");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "FirstDate");
    }
}
