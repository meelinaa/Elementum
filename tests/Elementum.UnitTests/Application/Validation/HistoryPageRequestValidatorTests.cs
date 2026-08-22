using Elementum.Application.Requests;
using Elementum.Application.Validation;

namespace Elementum.Application.Tests.Validation;

public class HistoryPageRequestValidatorTests
{
    private readonly HistoryPageRequestValidator _validator = new();

    // [R]IGHT-BICEP: default skip/take (omitted take) is a valid page request
    [Fact]
    public void Validate_DefaultPage_PassesValidation()
    {
        var result = _validator.Validate(new HistoryPageRequest());

        Assert.True(result.IsValid);
    }

    // RIGHT-[B]ICEP: take above MaxTake is still valid — the use case clamps instead of returning 400
    [Fact]
    public void Validate_WhenTakeExceedsHardCap_PassesValidation()
    {
        var result = _validator.Validate(new HistoryPageRequest { Skip = 0, Take = HistoryQueryLimits.MaxTake + 1 });

        Assert.True(result.IsValid);
    }

    // RIGHT-BIC[E]P: negative skip is rejected at the HTTP boundary
    [Fact]
    public void Validate_WhenSkipNegative_FailsValidation()
    {
        var result = _validator.Validate(new HistoryPageRequest { Skip = -1 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Skip");
    }

    // RIGHT-BIC[E]P: explicit take of 0 is rejected
    [Fact]
    public void Validate_WhenTakeZero_FailsValidation()
    {
        var result = _validator.Validate(new HistoryPageRequest { Take = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Take");
    }
}
