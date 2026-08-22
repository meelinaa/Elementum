namespace Elementum.Cli.Exceptions;

/// <summary>
/// Base class for CLI-level exceptions.
/// </summary>
public abstract class CliException : Exception
{
    protected CliException(string message) : base(message) { }
    protected CliException(string message, Exception innerException) : base(message, innerException) { }
}
