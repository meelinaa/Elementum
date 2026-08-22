using Elementum.Api.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.Api.Exceptions;

/// <summary>
/// Global exception handler that maps unhandled exceptions to RFC 7807 ProblemDetails responses.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ApiLogMessages.UnhandledExceptionOccurred(
            logger,
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception);

        var mapping = ExceptionStatusMapper.Map(exception);

        httpContext.Response.StatusCode = mapping.StatusCode;

        // Provide dynamic Retry-After header for transient upstream errors and rate limits
        if (mapping.StatusCode is StatusCodes.Status503ServiceUnavailable or StatusCodes.Status504GatewayTimeout)
        {
            httpContext.Response.Headers.RetryAfter = "5";
        }
        else if (mapping.StatusCode == StatusCodes.Status429TooManyRequests)
        {
            httpContext.Response.Headers.RetryAfter = "60";
        }

        var problemDetails = mapping.ToProblemDetails(
            detail: environment.IsDevelopment() ? exception.Message : null,
            instance: $"{httpContext.Request.Method} {httpContext.Request.Path}");

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}
