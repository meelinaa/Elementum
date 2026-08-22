using Microsoft.AspNetCore.Mvc;

namespace Elementum.Api.Exceptions;

/// <summary>
/// HTTP status and RFC 7807 metadata derived from an unhandled exception.
/// </summary>
public sealed record ExceptionMappingResult(
    int StatusCode,
    string Title,
    string? TypeUri)
{
    /// <summary>
    /// Builds a ProblemDetails document with the mapped status, title, and type URI.
    /// Used by the global exception handler and by controllers that return mapped errors without throwing.
    /// </summary>
    public ProblemDetails ToProblemDetails(string? detail = null, string? instance = null) =>
        new()
        {
            Status = StatusCode,
            Title = Title,
            Type = TypeUri,
            Detail = detail,
            Instance = instance
        };
}

