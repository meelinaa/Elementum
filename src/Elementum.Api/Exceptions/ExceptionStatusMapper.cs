using Elementum.Application.Exceptions;
using Elementum.Domain.Exceptions;

namespace Elementum.Api.Exceptions;

/// <summary>
/// Maps unhandled exceptions to HTTP status codes and RFC 7807 problem metadata.
/// </summary>
public static class ExceptionStatusMapper
{
    private const string TitleInternalError = "An error occurred";
    private const string TitleInvalidRequest = "Invalid request";
    private const string TitleDomainValidation = "Domain validation error";
    private const string TitleUpstreamError = "Upstream service error";
    private const string TitleServiceUnavailable = "Service unavailable";
    private const string TitleResourceNotFound = "Resource not found";
    private const string TitleRequestCancelled = "Request cancelled";
    private const string TitleGatewayTimeout = "Gateway timeout";

    private static readonly IReadOnlyDictionary<int, string> StatusTypeUris = new Dictionary<int, string>
    {
        [StatusCodes.Status400BadRequest] = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        [StatusCodes.Status404NotFound] = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        [422] = "https://tools.ietf.org/html/rfc4918#section-11.2",
        [StatusCodes.Status500InternalServerError] = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        [StatusCodes.Status502BadGateway] = "https://tools.ietf.org/html/rfc7231#section-6.6.3",
        [StatusCodes.Status503ServiceUnavailable] = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
        [StatusCodes.Status504GatewayTimeout] = "https://tools.ietf.org/html/rfc7231#section-6.6.5"
    };

    public static ExceptionMappingResult Map(Exception exception) =>
        exception switch
        {
            TaskCanceledException => Map499(TitleRequestCancelled),
            OperationCanceledException => Map499(TitleRequestCancelled),
            TimeoutException => Map504(TitleGatewayTimeout),
            Polly.CircuitBreaker.BrokenCircuitException => Map503(TitleServiceUnavailable),
            KeyNotFoundException => Map404(TitleResourceNotFound),
            DomainException => Map422(TitleDomainValidation),
            ExternalApiException => Map502(TitleUpstreamError),
            ConfigurationException => Map500(TitleInternalError),
            Elementum.Application.Exceptions.ApplicationException => Map502(TitleUpstreamError),
            HttpRequestException => Map502(TitleUpstreamError),
            ArgumentOutOfRangeException => Map400(TitleInvalidRequest),
            ArgumentException => Map400(TitleInvalidRequest),
            _ => Map500(TitleInternalError)
        };

    /// <summary>
    /// Same 404 mapping as <see cref="KeyNotFoundException"/> so controllers do not invent a second ProblemDetails shape.
    /// </summary>
    public static ExceptionMappingResult ForNotFound() => Map(new KeyNotFoundException());

    private static ExceptionMappingResult Map400(string title) =>
        new(StatusCodes.Status400BadRequest, title, GetTypeUri(StatusCodes.Status400BadRequest));

    private static ExceptionMappingResult Map404(string title) =>
        new(StatusCodes.Status404NotFound, title, GetTypeUri(StatusCodes.Status404NotFound));

    private static ExceptionMappingResult Map422(string title) =>
        new(422, title, GetTypeUri(422));

    private static ExceptionMappingResult Map503(string title) =>
        new(StatusCodes.Status503ServiceUnavailable, title, GetTypeUri(StatusCodes.Status503ServiceUnavailable));

    /// <summary>
    /// Maps client-initiated request aborts (via <see cref="OperationCanceledException"/> or <see cref="TaskCanceledException"/>)
    /// to non-standard HTTP 499 (Client Closed Request).
    /// <para>
    /// <b>Rationale:</b> When a caller terminates the connection prior to response completion, mapping to HTTP 500 (Internal Server Error)
    /// or 504 (Gateway Timeout) would corrupt service SLOs/metrics by reporting client cancellations as server failures.
    /// HTTP 499 is the industry-standard de-facto code (originated by NGINX and recognized by AWS ALB/Cloudflare/Envoy).
    /// Since 499 is non-RFC (not specified in IETF RFC 7231/9110), the <c>type</c> URI is intentionally left <c>null</c>.
    /// </para>
    /// </summary>
    private static ExceptionMappingResult Map499(string title) =>
        new(499, title, null);

    private static ExceptionMappingResult Map500(string title) =>
        new(StatusCodes.Status500InternalServerError, title, GetTypeUri(StatusCodes.Status500InternalServerError));

    private static ExceptionMappingResult Map502(string title) =>
        new(StatusCodes.Status502BadGateway, title, GetTypeUri(StatusCodes.Status502BadGateway));

    private static ExceptionMappingResult Map504(string title) =>
        new(StatusCodes.Status504GatewayTimeout, title, GetTypeUri(StatusCodes.Status504GatewayTimeout));

    private static string? GetTypeUri(int statusCode) =>
        StatusTypeUris.TryGetValue(statusCode, out var typeUri) ? typeUri : null;
}
