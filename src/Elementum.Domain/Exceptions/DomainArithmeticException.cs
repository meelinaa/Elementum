namespace Elementum.Domain.Exceptions;

/// <summary>
/// Thrown when an invalid domain arithmetic operation is attempted (e.g. division by zero).
/// </summary>
public sealed class DomainArithmeticException : DomainException
{
    private DomainArithmeticException(string message) : base(message) { }

    public static DomainArithmeticException DivideByZero(string message) => new(message);
}
