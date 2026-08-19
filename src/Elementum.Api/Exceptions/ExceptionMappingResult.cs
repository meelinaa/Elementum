namespace Elementum.Api.Exceptions;

/// <summary>
/// HTTP status and RFC 7807 metadata derived from an unhandled exception.
/// </summary>
public sealed record ExceptionMappingResult(
    int StatusCode,
    string Title,
    string? TypeUri);
