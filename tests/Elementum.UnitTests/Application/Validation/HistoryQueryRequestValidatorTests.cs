using Elementum.Application.Requests;
using Elementum.Application.Validation;

namespace Elementum.Application.Tests.Validation;

public class HistoryQueryRequestValidatorTests
{
    private readonly HistoryQueryRequestValidator _validator = new();

    // [R]IGHT-BICEP: omitted currency, dates, and take is a valid history query
    [Fact]
    public void Validate_WhenDefaults_PassesValidation()
    {
        Assert.True(_validator.Validate(new HistoryQueryRequest()).IsValid);
    }

    // RIGHT-BIC[E]P: unsupported currency fails on the combined history request, not as a raw query string
    [Fact]
    public void Validate_WhenCurrencyUnsupported_FailsOnCurrency()
    {
        var result = _validator.Validate(new HistoryQueryRequest { Currency = "GBP" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(HistoryQueryRequest.Currency));
    }

    // RIGHT-BIC[E]P: invalid from is still rejected after currency is folded into the same model
    [Fact]
    public void Validate_WhenFromInvalid_FailsOnFrom()
    {
        var result = _validator.Validate(new HistoryQueryRequest { From = "not-a-date" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(HistoryQueryRequest.From));
    }
}
