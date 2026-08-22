using Elementum.Application.Requests;
using Elementum.Domain.Constants;

namespace Elementum.Application.Tests.Validation;

public class CurrencyRequestTests
{
    // RIGHT-[B]ICEP: omitted currency is null for history (all codes) and EUR for trading
    [Fact]
    public void NormalizedOrNull_WhenOmitted_ReturnsNull()
    {
        Assert.Null(new CurrencyRequest().NormalizedOrNull());
        Assert.Null(new CurrencyRequest("  ").NormalizedOrNull());
    }

    // [R]IGHT-BICEP: trading default is EUR so controllers do not hard-code the ISO code
    [Fact]
    public void ForTrading_WhenOmitted_ReturnsEur()
    {
        Assert.Equal(DomainConstants.Currencies.Eur, new CurrencyRequest().ForTrading());
        Assert.Equal(DomainConstants.Currencies.Usd, new CurrencyRequest("usd").ForTrading());
        Assert.Equal(DomainConstants.Currencies.Usd, new CurrencyRequest("usd").NormalizedOrNull());
    }
}
