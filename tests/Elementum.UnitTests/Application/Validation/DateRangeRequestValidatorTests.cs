using Elementum.Application.Requests;
using Elementum.Application.Validation;

namespace Elementum.Application.Tests.Validation;

public class DateRangeRequestValidatorTests
{
    private readonly DateRangeRequestValidator _validator = new();

    // [R]IGHT-BICEP: Verifies that a valid chronological start-to-end date range passes validation
    [Fact]
    public void Validate_ValidChronologicalRange_PassesValidation()
    {
        // Arrange
        var request = new DateRangeRequest("2024-01-01", "2024-01-15");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // [B]OUNDARY: Verifies that identical start and end dates (single day query) pass validation
    [Fact]
    public void Validate_SingleDayRange_PassesValidation()
    {
        // Arrange
        var request = new DateRangeRequest("2024-01-15", "2024-01-15");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // [E]RROR: Verifies that StartDate after EndDate triggers validation failure
    [Fact]
    public void Validate_WhenStartAfterEnd_FailsValidation()
    {
        // Arrange
        var request = new DateRangeRequest("2024-01-20", "2024-01-10");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("FirstDate cannot be after LastDate"));
    }

    // [E]RROR: Verifies that invalid date formats (non ISO-8601) fail validation
    [Fact]
    public void Validate_WhenInvalidDateFormat_FailsValidation()
    {
        // Arrange
        var request = new DateRangeRequest("20-01-2024", "not-a-date");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    // [E]RROR: Verifies that dates in the future fail validation
    [Fact]
    public void Validate_WhenLastDateInFuture_FailsValidation()
    {
        // Arrange
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd");
        var request = new DateRangeRequest("2024-01-01", future);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("future"));
    }
}
