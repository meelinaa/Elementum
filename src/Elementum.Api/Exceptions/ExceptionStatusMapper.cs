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
            KeyNotFoundException => Map404(TitleResourceNotFound),
            CurrencyMismatchException => Map422(TitleDomainValidation),
            ResultException => Map500(TitleInternalError),
            DomainException => Map422(TitleDomainValidation),
            ExternalApiException => Map502(TitleUpstreamError),
            ConfigurationException => Map500(TitleInternalError),
            Elementum.Application.Exceptions.ApplicationException => Map502(TitleUpstreamError),
            HttpRequestException => Map502(TitleUpstreamError),
            ArgumentOutOfRangeException => Map400(TitleInvalidRequest),
            ArgumentException => Map400(TitleInvalidRequest),
            _ => Map500(TitleInternalError)
        };

    private static ExceptionMappingResult Map400(string title) =>
        new(StatusCodes.Status400BadRequest, title, GetTypeUri(StatusCodes.Status400BadRequest));

    private static ExceptionMappingResult Map404(string title) =>
        new(StatusCodes.Status404NotFound, title, GetTypeUri(StatusCodes.Status404NotFound));

    private static ExceptionMappingResult Map422(string title) =>
        new(422, title, GetTypeUri(422));

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
