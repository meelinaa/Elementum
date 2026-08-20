using System.Net;
using System.Text;
using Elementum.Application.Options;
using Elementum.Infrastructure.Outbound.External;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Elementum.UnitTests.Infrastructure.External;

public class GoldApiHealthCheckTests
{
    private static IHttpClientFactory CreateHttpClientFactory(HttpStatusCode statusCode)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factory.Object;
    }

    // [R]IGHT-BICEP: Verifies that GoldApiHealthCheck returns Healthy when endpoint responds with HTTP 200
    [Fact]
    public async Task CheckHealthAsync_WhenEndpointReturns200_ReturnsHealthy()
    {
        // Arrange
        var factory = CreateHttpClientFactory(HttpStatusCode.OK);
        var options = Options.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var healthCheck = new GoldApiHealthCheck(factory, options, NullLogger<GoldApiHealthCheck>.Instance);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Edelmetalle API is available.", result.Description);
    }

    // [B]OUNDARY: Verifies that missing BaseUrl returns Degraded without throwing exceptions
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CheckHealthAsync_WhenBaseUrlMissing_ReturnsDegraded(string? missingUrl)
    {
        // Arrange
        var factory = CreateHttpClientFactory(HttpStatusCode.OK);
        var options = Options.Create(new MetalsApiOptions { BaseUrl = missingUrl! });
        var healthCheck = new GoldApiHealthCheck(factory, options, NullLogger<GoldApiHealthCheck>.Instance);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Metals API BaseUrl is not configured.", result.Description);
    }

    // [E]RROR: Verifies that non-success HTTP status codes return Degraded
    [Fact]
    public async Task CheckHealthAsync_WhenEndpointReturns503_ReturnsDegraded()
    {
        // Arrange
        var factory = CreateHttpClientFactory(HttpStatusCode.ServiceUnavailable);
        var options = Options.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var healthCheck = new GoldApiHealthCheck(factory, options, NullLogger<GoldApiHealthCheck>.Instance);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("ServiceUnavailable", result.Description);
    }

    // [E]RROR: Verifies that network/connection exceptions return Unhealthy with exception details
    [Fact]
    public async Task CheckHealthAsync_WhenHttpThrowsException_ReturnsUnhealthy()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var options = Options.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var healthCheck = new GoldApiHealthCheck(factory.Object, options, NullLogger<GoldApiHealthCheck>.Instance);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
        Assert.Equal("Connection refused", result.Exception.Message);
    }
}
