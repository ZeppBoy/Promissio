using System.Globalization;

namespace Promissio.Domain.ValueObjects;

/// <summary>
/// Immutable representation of a percentage value.
/// </summary>
/// <remarks>
/// Stores percentage as a decimal fraction (5% = 0.05).
/// Enforces non-negative invariant.
/// </remarks>
public sealed record Percentage
{
    /// <summary>
    /// The decimal fraction representation of the percentage (e.g., 5% = 0.05).
    /// </summary>
    public Decimal Fraction { get; }

    /// <summary>
    /// Creates a new Percentage with validation.
    /// </summary>
    public Percentage(Decimal fraction)
    {
        if (fraction < 0)
            throw new ArgumentOutOfRangeException(nameof(fraction), "Percent must be non-negative.");

        Fraction = fraction;
    }

    /// <summary>
    /// Gets the percentage as a percent value (e.g., 5 for 5%).
    /// </summary>
    public Decimal AsPercent => Fraction * 100m;

    /// <summary>
    /// Gets the percentage as basis points (e.g., 500 for 5%).
    /// </summary>
    public long AsBasisPoints => (long)(Fraction * 10000m);

    /// <summary>
    /// Creates a Percentage from a percent value (e.g., 5 for 5%).
    /// </summary>
    public static Percentage FromPercent(Decimal percent)
    {
        if (percent < 0)
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be non-negative.");

        return new Percentage(percent / 100m);
    }

    /// <summary>
    /// Creates a Percentage from basis points (e.g., 500 for 5%).
    /// </summary>
    public static Percentage FromBasisPoints(Decimal basisPoints)
    {
        if (basisPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(basisPoints), "Basis points must be non-negative.");

        return new Percentage(basisPoints / 10000m);
    }

    /// <summary>
    /// Creates a Percentage from basis points (long) (e.g., 500 for 5%).
    /// </summary>
    public static Percentage FromBasisPoints(long basisPoints)
    {
        if (basisPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(basisPoints), "Basis points must be non-negative.");

        return new Percentage((Decimal)basisPoints / 10000m);
    }

    /// <summary>
    /// Creates a Percentage from a decimal fraction (e.g., 0.05 for 5%).
    /// </summary>
    public static Percentage FromFraction(Decimal fraction)
    {
        if (fraction < 0)
            throw new ArgumentOutOfRangeException(nameof(fraction), "Fraction must be non-negative.");

        return new Percentage(fraction);
    }

    #region Arithmetic

    public static Percentage operator +(Percentage left, Percentage right)
    {
        return new Percentage(left.Fraction + right.Fraction);
    }

    public static Percentage operator -(Percentage left, Percentage right)
    {
        var result = left.Fraction - right.Fraction;
        if (result < 0)
            throw new InvalidOperationException("Result of subtraction would be negative.");
        return new Percentage(result);
    }

    public static Percentage operator *(Percentage percentage, Decimal factor)
    {
        var result = percentage.Fraction * factor;
        if (result < 0)
            throw new InvalidOperationException("Result of multiplication would be negative.");
        return new Percentage(result);
    }

    public static Percentage operator *(Decimal factor, Percentage percentage) => percentage * factor;

    public static Percentage operator /(Percentage percentage, Decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide percentage by zero.");

        return new Percentage(percentage.Fraction / divisor);
    }

    #endregion

    public override string ToString()
    {
        // Convert to string and trim trailing zeros for clean display
        var percentStr = AsPercent.ToString(CultureInfo.InvariantCulture);
        var trimmed = percentStr.TrimEnd('0').TrimEnd('.');
        return $"{trimmed}%";
    }
}
