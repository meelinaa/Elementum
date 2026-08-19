namespace Elementum.Domain.Exceptions;

/// <summary>
/// Abstract base class for all domain-specific exceptions in Elementum.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}
