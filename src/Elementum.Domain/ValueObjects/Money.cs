using System.Globalization;
using Elementum.Domain.Exceptions;

namespace Elementum.Domain.ValueObjects;

/// <summary>
/// Immutable Value Object representing a monetary amount with an associated Currency (EUR or USD).
/// Enforces precision, currency consistency, and financial rounding rules.
/// </summary>
public readonly record struct Money : IComparable<Money>, IEquatable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money From(decimal amount, Currency currency) => new(amount, currency);
    public static Money From(decimal amount, string currencyCode) => new(amount, Currency.FromCode(currencyCode));

    public static Money Zero(Currency currency) => new(0m, currency);
    public static Money Usd(decimal amount) => new(amount, Currency.USD);
    public static Money Eur(decimal amount) => new(amount, Currency.EUR);

    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money a, decimal multiplier) =>
        new(a.Amount * multiplier, a.Currency);

    public static Money operator *(decimal multiplier, Money a) =>
        new(a.Amount * multiplier, a.Currency);

    public static Money operator /(Money a, decimal divisor)
    {
        DomainThrowHelper.ThrowIfZero(divisor, "Cannot divide Money by zero.");
        return new Money(a.Amount / divisor, a.Currency);
    }

    public static bool operator <(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount < b.Amount;
    }

    public static bool operator >(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount > b.Amount;
    }

    public static bool operator <=(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount <= b.Amount;
    }

    public static bool operator >=(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount >= b.Amount;
    }

    /// <summary>
    /// Performs standard Banker's Rounding (MidpointRounding.ToEven) to the given decimal precision.
    /// </summary>
    public Money Round(int decimals = 4, MidpointRounding mode = MidpointRounding.ToEven) =>
        new(Math.Round(Amount, decimals, mode), Currency);

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public override string ToString() =>
        $"{Amount.ToString($"F{Currency.DecimalPlaces}", CultureInfo.InvariantCulture)} {Currency.Code}";

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw CurrencyMismatchException.For(a.Currency.Code, b.Currency.Code);
    }
}
