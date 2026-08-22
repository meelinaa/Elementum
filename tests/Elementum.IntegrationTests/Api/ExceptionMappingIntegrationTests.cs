using System.Net;
using System.Net.Http.Json;
using Elementum.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.IntegrationTests.Api;

public class ExceptionMappingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ExceptionMappingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // RIGHT-[B]ICEP: symbol at MaxLength(15)+1 must be rejected with 400 before controller execution
    [Fact]
    public async Task GetPriceHistory_WhenSymbolExceedsMaxLength_Returns400ValidationProblemDetails()
    {
        // Arrange
        const string tooLongSymbol = "ABCDEFGHIJKLMNOPQ";

        // Act
        var response = await _client.GetAsync($"/api/v1/history/{tooLongSymbol}?currency=USD");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", problem.Type);
    }

    // [R]IGHT-BICEP: unknown trading symbol returns 404 ProblemDetails via controller null-check, not 200 or 500
    [Fact]
    public async Task GetLiveTradingAnalysis_WhenSymbolNotInDatabase_Returns404ProblemDetails()
    {
        // Arrange
        const string unknownSymbol = "NONEXISTENT";

        // Act
        var response = await _client.GetAsync($"/api/v1/prices/live/trading/{unknownSymbol}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Resource not found", problem.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", problem.Type);
    }

    // RIGHT-BIC[E]P: currency other than USD/EUR is rejected at the HTTP boundary (400), analog to SymbolRequest
    [Theory]
    [InlineData("EURO")]
    [InlineData("GBP")]
    public async Task GetLiveTradingAnalysis_WhenCurrencyUnsupported_Returns400ValidationProblemDetails(string currency)
    {
        var response = await _client.GetAsync($"/api/v1/prices/live/trading/XAU?currency={currency}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", problem.Type);
        Assert.True(problem.Errors.ContainsKey("Currency"));
    }

    // RIGHT-BIC[E]P: history uses the same CurrencyRequest validator as trading
    [Fact]
    public async Task GetPriceHistory_WhenCurrencyUnsupported_Returns400ValidationProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/history/XAU?currency=GBP");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", problem.Type);
        Assert.True(problem.Errors.ContainsKey("Currency"));
    }
}

public class UpstreamFailureExceptionMappingIntegrationTests : IClassFixture<UpstreamFailureWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UpstreamFailureExceptionMappingIntegrationTests(UpstreamFailureWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // RIGHT-BIC[E]P: empty upstream response propagates as 502 through GlobalExceptionHandler, not 500
    [Fact]
    public async Task GetLivePrices_WhenUpstreamReturnsEmpty_Returns502ProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/prices/live");

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(502, problem.Status);
        Assert.Equal("Upstream service error", problem.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.3", problem.Type);
    }
}

public class UpstreamTimeoutExceptionMappingIntegrationTests : IClassFixture<UpstreamTimeoutWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UpstreamTimeoutExceptionMappingIntegrationTests(UpstreamTimeoutWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // RIGHT-BIC[E]P: upstream timeout propagates as 504 gateway timeout through the HTTP pipeline
    [Fact]
    public async Task GetLivePrices_WhenUpstreamTimesOut_Returns504ProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/prices/live");

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(504, problem.Status);
        Assert.Equal("Gateway timeout", problem.Title);
    }
}

public class UpstreamUnexpectedFailureExceptionMappingIntegrationTests : IClassFixture<UpstreamUnexpectedFailureWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UpstreamUnexpectedFailureExceptionMappingIntegrationTests(UpstreamUnexpectedFailureWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // RIGHT-BIC[E]P: unmapped upstream exceptions fall back to 500 internal server error
    [Fact]
    public async Task GetLivePrices_WhenUpstreamThrowsUnexpectedException_Returns500ProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/prices/live");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An error occurred", problem.Title);
    }
}
