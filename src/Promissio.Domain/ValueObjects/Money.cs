using System.Globalization;

namespace Promissio.Domain.ValueObjects;

/// <summary>
/// Immutable representation of a monetary amount with an associated currency.
/// </summary>
/// <remarks>
/// Money is a value object: equality is based on amount and currency, not reference identity.
/// All arithmetic operations enforce same-currency constraint.
/// Uses decimal internally to avoid floating-point rounding issues.
/// </remarks>
public sealed class Money(Decimal amount, string currency) : IEquatable<Money>
{
    public Decimal Amount { get; } = Math.Round(amount, 2, MidpointRounding.ToEven);

    public string Currency { get; } = currency ?? throw new ArgumentNullException(nameof(currency));

    public static Money Zero(string currency) => new(0m, currency);

    #region Arithmetic

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot add {left.Currency} to {right.Currency}");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot subtract {right.Currency} from {left.Currency}");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, Decimal factor)
    {
        return new Money(money.Amount * factor, money.Currency);
    }

    public static Money operator *(Decimal factor, Money money) => money * factor;

    public static Money operator /(Money money, Decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero.");

        return new Money(money.Amount / divisor, money.Currency);
    }

    #endregion

    #region Comparison

    public static bool operator ==(Money? left, Money? right) => Equals(left, right);

    public static bool operator !=(Money? left, Money? right) => !Equals(left, right);

    public static bool operator <(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare {left.Currency} with {right.Currency}");

        return left.Amount < right.Amount;
    }

    public static bool operator >(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare {left.Currency} with {right.Currency}");

        return left.Amount > right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare {left.Currency} with {right.Currency}");

        return left.Amount <= right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare {left.Currency} with {right.Currency}");

        return left.Amount >= right.Amount;
    }

    #endregion

    public bool Equals(Money? other) => other is not null && this.Amount == other.Amount && this.Currency == other.Currency;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";
}
