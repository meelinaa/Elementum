using Elementum.Application.Requests;
using Elementum.Application.Validation;

namespace Elementum.Application.Tests.Validation;

public class DateRangeRequestValidatorTests
{
    private readonly DateRangeRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidChronologicalRange_PassesValidation()
    {
        var request = new DateRangeRequest("2024-01-01", "2024-01-15");
        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenStartAfterEnd_FailsValidation()
    {
        var request = new DateRangeRequest("2024-01-20", "2024-01-10");
        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("FirstDate cannot be after LastDate"));
    }

    [Fact]
    public void Validate_WhenInvalidDateFormat_FailsValidation()
    {
        var request = new DateRangeRequest("20-01-2024", "not-a-date");
        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenLastDateInFuture_FailsValidation()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd");
        var request = new DateRangeRequest("2024-01-01", future);
        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("future"));
    }
}
