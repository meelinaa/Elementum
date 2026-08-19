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

    // [B]OUNDARY RIGHT-BICEP: symbol at MaxLength(15)+1 must be rejected with 400 before controller execution
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
    }
}

public class UpstreamFailureExceptionMappingIntegrationTests : IClassFixture<UpstreamFailureWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UpstreamFailureExceptionMappingIntegrationTests(UpstreamFailureWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // [E]RROR RIGHT-BICEP: empty upstream response propagates as 502 through GlobalExceptionHandler, not 500
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
