namespace Elementum.Application.Exceptions;

/// <summary>
/// Thrown when the upstream metals payload fails FluentValidation and must not be persisted.
/// </summary>
public sealed class UpstreamValidationException : ApplicationException
{
    private UpstreamValidationException(string message) : base(message) { }

    public static UpstreamValidationException FromErrors(string errors) =>
        new($"Upstream Edelmetalle API data validation failed: {errors}");
}
