using System.Net;

namespace Elementum.Cli.Exceptions;

/// <summary>
/// Thrown when an HTTP request made by the CLI fails.
/// </summary>
public sealed class CliHttpException : CliException
{
    public HttpStatusCode StatusCode { get; }

    private CliHttpException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public static CliHttpException RequestFailed(HttpStatusCode statusCode, string endpoint) =>
        new(statusCode, $"Request to '{endpoint}' failed with status code {statusCode}.");
}
