namespace Elementum.Application.Exceptions;

/// <summary>
/// Abstract base class for all application-level exceptions in Elementum.
/// </summary>
public abstract class ApplicationException : Exception
{
    protected ApplicationException(string message) : base(message) { }
    protected ApplicationException(string message, Exception innerException) : base(message, innerException) { }
}
