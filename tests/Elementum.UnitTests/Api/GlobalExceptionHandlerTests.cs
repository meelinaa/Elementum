using Elementum.Api.Exceptions;
using Elementum.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elementum.UnitTests.Api;

public class GlobalExceptionHandlerTests
{
    private ProblemDetailsContext? _capturedContext;

    // [R]IGHT-BICEP: handler must write mapped status, title, type, and request instance into ProblemDetails
    [Fact]
    public async Task TryHandleAsync_SetsProblemDetailsFromMappedException()
    {
        // Arrange
        var handler = CreateHandler(environmentName: Environments.Production);
        var context = CreateHttpContext("GET", "/api/v1/prices/live");
        var exception = ExternalApiException.EmptyLiveQuoteResponse();

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
        Assert.NotNull(_capturedContext);
        Assert.Equal(502, _capturedContext!.ProblemDetails.Status);
        Assert.Equal("Upstream service error", _capturedContext.ProblemDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.3", _capturedContext.ProblemDetails.Type);
        Assert.Equal("GET /api/v1/prices/live", _capturedContext.ProblemDetails.Instance);
    }

    // [B]OUNDARY RIGHT-BICEP: production responses must omit exception detail for security
    [Fact]
    public async Task TryHandleAsync_WhenProduction_OmitsExceptionDetail()
    {
        // Arrange
        var handler = CreateHandler(environmentName: Environments.Production);
        var context = CreateHttpContext("POST", "/api/v1/history/XAU");
        var exception = new Exception("internal database connection string");

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.Null(_capturedContext!.ProblemDetails.Detail);
    }

    // [B]OUNDARY RIGHT-BICEP: development responses include exception message as detail for diagnostics
    [Fact]
    public async Task TryHandleAsync_WhenDevelopment_IncludesExceptionMessageAsDetail()
    {
        // Arrange
        var handler = CreateHandler(environmentName: Environments.Development);
        var context = CreateHttpContext("GET", "/api/v1/prices/live");
        const string message = "External metals API returned an empty live quote response.";
        var exception = ExternalApiException.EmptyLiveQuoteResponse();

        // Act
        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.Equal(message, _capturedContext!.ProblemDetails.Detail);
    }

    private GlobalExceptionHandler CreateHandler(string environmentName)
    {
        _capturedContext = null;

        var environment = new MockHostingEnvironment { EnvironmentName = environmentName };
        var problemDetailsService = new CapturingProblemDetailsService(ctx => _capturedContext = ctx);

        return new GlobalExceptionHandler(
            problemDetailsService,
            environment,
            NullLogger<GlobalExceptionHandler>.Instance);
    }

    private static DefaultHttpContext CreateHttpContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        return context;
    }

    private sealed class MockHostingEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Elementum.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class CapturingProblemDetailsService(Action<ProblemDetailsContext> capture) : IProblemDetailsService
    {
        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            capture(context);
            return ValueTask.CompletedTask;
        }

        public ValueTask WriteAsync(ProblemDetailsContext context, CancellationToken cancellationToken)
        {
            capture(context);
            return ValueTask.CompletedTask;
        }
    }
}
