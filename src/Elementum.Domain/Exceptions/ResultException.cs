namespace Elementum.Domain.Exceptions;

/// <summary>
/// Thrown when an invalid state transition or operation is attempted on a <see cref="Common.Result"/>.
/// </summary>
public sealed class ResultException : InvalidOperationException
{
    private ResultException(string message) : base(message) { }

    public static ResultException SuccessfulResultCannotHaveError() =>
        new("A successful result cannot have an error.");

    public static ResultException FailureResultMustSpecifyError() =>
        new("A failure result must specify an error.");

    public static ResultException CannotAccessValueOfFailure() =>
        new("The value of a failure result cannot be accessed.");
}
