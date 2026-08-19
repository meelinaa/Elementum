using System.Net;
using System.Net.Http.Json;
using Elementum.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Elementum.IntegrationTests.Api;

public class RateLimitingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RateLimitingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // [B]OUNDARY / [E]RROR: Verifies that exceeding the IP rate limit boundary returns HTTP 429 Too Many Requests
    [Fact]
    public async Task RateLimiter_WhenPermitLimitExceeded_Returns429TooManyRequests()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:PermitLimit"] = "2",
                    ["RateLimiting:WindowSeconds"] = "60",
                    ["RateLimiting:QueueLimit"] = "0"
                });
            });
        }).CreateClient();

        // Act - 1st request (200 OK)
        var res1 = await client.GetAsync("/api/v1/prices/live");

        // Act - 2nd request (200 OK)
        var res2 = await client.GetAsync("/api/v1/prices/live");

        // Act - 3rd request (429 Rate Limited)
        var res3 = await client.GetAsync("/api/v1/prices/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, res3.StatusCode);

        var problem = await res3.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status429TooManyRequests, problem.Status);
        Assert.Equal("Too Many Requests", problem.Title);
    }
}
