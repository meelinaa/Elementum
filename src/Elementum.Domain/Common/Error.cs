namespace Elementum.Domain.Common;

/// <summary>
/// Represents a domain or validation error with a unique error code and a human-readable message.
/// </summary>
public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");

    public bool IsEmpty => string.IsNullOrEmpty(Code);

    public static implicit operator string(Error error) => error.Code;

    public override string ToString() => string.IsNullOrEmpty(Code) ? string.Empty : $"{Code}: {Message}";
}
