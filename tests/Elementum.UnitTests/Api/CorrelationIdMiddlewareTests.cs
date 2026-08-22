using Elementum.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace Elementum.UnitTests.Api;

public class CorrelationIdMiddlewareTests
{
    // [R]IGHT-BICEP: X-Correlation-Id header is propagated to context and response headers
    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdHeaderPresent_UsesProvidedCorrelationId()
    {
        // Arrange
        const string expectedId = "custom-correlation-123";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName] = expectedId;

        var nextInvoked = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextInvoked);
        Assert.Equal(expectedId, context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
    }

    // [B]OUNDARY RIGHT-BICEP: X-Request-Id header is used as secondary fallback when X-Correlation-Id is missing
    [Fact]
    public async Task InvokeAsync_WhenOnlyRequestIdHeaderPresent_FallsBackToRequestId()
    {
        // Arrange
        const string expectedId = "request-id-456";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.RequestIdHeaderName] = expectedId;

        var nextInvoked = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextInvoked);
        Assert.Equal(expectedId, context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
    }

    // [B]OUNDARY RIGHT-BICEP: generates a valid 32-character GUID when no headers are supplied
    [Fact]
    public async Task InvokeAsync_WhenNoCorrelationHeaders_GeneratesNewGuid()
    {
        // Arrange
        var context = new DefaultHttpContext();

        var nextInvoked = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextInvoked);
        var generatedId = context.Items[CorrelationIdMiddleware.HttpContextItemKey]?.ToString();
        Assert.NotNull(generatedId);
        Assert.Equal(32, generatedId.Length);
    }
}
