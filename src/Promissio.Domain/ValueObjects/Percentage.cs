using System.Globalization;

namespace Promissio.Domain.ValueObjects;

/// <summary>
/// Immutable representation of a percentage value with explicit unit.
/// </summary>
/// <remarks>
/// Stores internally as a decimal fraction (e.g., 5% = 0.05m).
/// Provides conversion methods for basis points, fractions, and percent.
/// </remarks>
public sealed class Percentage(Decimal fraction) : IEquatable<Percentage>
{
    public Decimal Fraction { get; } = fraction;

    public Decimal AsPercent => Fraction * 100m;

    public long AsBasisPoints => (long)Math.Round(Fraction * 10000m, MidpointRounding.ToEven);

    public static Percentage FromPercent(Decimal percent)
    {
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be between 0 and 100.");

        return new Percentage(percent / 100m);
    }

    public static Percentage FromBasisPoints(long basisPoints)
    {
        if (basisPoints < 0 || basisPoints > 10000)
            throw new ArgumentOutOfRangeException(nameof(basisPoints), "Basis points must be between 0 and 10000.");

        return new Percentage(basisPoints / 10000m);
    }

    public static Percentage FromFraction(Decimal fraction)
    {
        if (fraction < 0 || fraction > 1)
            throw new ArgumentOutOfRangeException(nameof(fraction), "Fraction must be between 0 and 1.");

        return new Percentage(fraction);
    }

    #region Arithmetic

    public static Percentage operator +(Percentage left, Percentage right) => new(left.Fraction + right.Fraction);

    public static Percentage operator -(Percentage left, Percentage right) => new(left.Fraction - right.Fraction);

    public static Percentage operator *(Percentage percentage, Decimal factor) => new(percentage.Fraction * factor);

    public static Percentage operator /(Percentage percentage, Decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide percentage by zero.");

        return new(percentage.Fraction / divisor);
    }

    #endregion

    public bool Equals(Percentage? other) => other is not null && this.Fraction == other.Fraction;

    public static bool operator ==(Percentage? left, Percentage? right) => Equals(left, right);

    public static bool operator !=(Percentage? left, Percentage? right) => !Equals(left, right);

    public override bool Equals(object? obj) => Equals(obj as Percentage);

    public override int GetHashCode() => HashCode.Combine(Fraction);

    public override string ToString() => $"{AsPercent.ToString("F4", CultureInfo.InvariantCulture)}%";
}
