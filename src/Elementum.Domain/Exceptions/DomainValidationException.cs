namespace Elementum.Domain.Exceptions;

/// <summary>
/// Thrown when a generic domain validation rule is violated (e.g. empty or whitespace string).
/// </summary>
public sealed class DomainValidationException : DomainException
{
    private DomainValidationException(string message) : base(message) { }

    public static DomainValidationException NullOrWhitespace(string paramName) =>
        new($"Parameter '{paramName}' cannot be null, empty, or whitespace.");
}
