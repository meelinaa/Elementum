using System.Diagnostics.CodeAnalysis;
using Elementum.Domain.Exceptions;

namespace Elementum.Domain.Common;

/// <summary>
/// Represents the outcome of an operation without a return value.
/// Uses <see cref="ResultException"/> factories for invariant validation.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw ResultException.SuccessfulResultCannotHaveError();
        if (!isSuccess && error == Error.None)
            throw ResultException.FailureResultMustSpecifyError();

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>
/// Represents the outcome of an operation returning a value of type <typeparamref name="TValue"/>.
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw ResultException.CannotAccessValueOfFailure();

    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        if (IsSuccess && _value is not null)
        {
            value = _value;
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}
