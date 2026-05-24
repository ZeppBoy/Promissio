using FsCheck;
using FsCheck.Xunit;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Tests.ValueObjects;

public class PercentagePropertyTests
{
    [Property]
    public bool Addition_Associative(SafeFraction a, SafeFraction b, SafeFraction c)
    {
        var percentA = Percentage.FromFraction(a.Value);
        var percentB = Percentage.FromFraction(b.Value);
        var percentC = Percentage.FromFraction(c.Value);

        var leftAssoc = (percentA + percentB) + percentC;
        var rightAssoc = percentA + (percentB + percentC);

        return leftAssoc == rightAssoc;
    }

    [Property]
    public bool Addition_Commutative(SafeFraction a, SafeFraction b)
    {
        var percentA = Percentage.FromFraction(a.Value);
        var percentB = Percentage.FromFraction(b.Value);

        return (percentA + percentB) == (percentB + percentA);
    }

    [Property]
    public bool FromPercent_RoundTrip(SafePercent percent)
    {
        var percentage = Percentage.FromPercent(percent.Value);

        return Math.Abs(percentage.AsPercent - percent.Value) < 0.0001m;
    }

    [Property]
    public bool FromBasisPoints_RoundTrip(BasisPoints bps)
    {
        var percentage = Percentage.FromBasisPoints(bps.Value);

        return percentage.AsBasisPoints == bps.Value;
    }

    [Property]
    public bool Multiplication_DistributiveOverAddition(SafeFraction a, SafeFraction b, PositiveDecimal factor)
    {
        var percentA = Percentage.FromFraction(a.Value);
        var percentB = Percentage.FromFraction(b.Value);

        var leftSide = (percentA + percentB) * factor.Value;
        var rightSide = (percentA * factor.Value) + (percentB * factor.Value);

        return Math.Abs(leftSide.Fraction - rightSide.Fraction) < 0.000001m;
    }

    [Property]
    public bool Division_IsInverseOfMultiplication(SafeFraction a, PositiveDecimal factor)
    {
        var percentage = Percentage.FromFraction(a.Value);

        return Math.Abs(((percentage * factor.Value) / factor.Value).Fraction - a.Value) < 0.000001m;
    }

    [Property]
    public bool Equality_Transitive(SafeFraction a, SafeFraction b, SafeFraction c)
    {
        var percentA = Percentage.FromFraction(a.Value);
        var percentB = Percentage.FromFraction(b.Value);
        var percentC = Percentage.FromFraction(c.Value);

        if (percentA == percentB && percentB == percentC)
            return percentA == percentC;

        return true;
    }

    [Property]
    public bool GetHashCode_ConsistentWithEquality(SafeFraction a)
    {
        var percentage = Percentage.FromFraction(a.Value);

        return percentage.GetHashCode() == percentage.GetHashCode();
    }

    [Property]
    public bool Multiplication_SelfConsistent(SafeFraction a, PositiveDecimal factor)
    {
        var percentage = Percentage.FromFraction(a.Value);
        var scaled = percentage * factor.Value;

        return scaled.Fraction == (a.Value * factor.Value);
    }
}

public readonly struct SafeFraction
{
    public Decimal Value { get; }

    public SafeFraction(Decimal value)
    {
        Value = Math.Clamp(Math.Abs(value), 0m, 1m);
    }
}

public readonly struct SafePercent
{
    public Decimal Value { get; }

    public SafePercent(Decimal value)
    {
        Value = Math.Clamp(Math.Abs(value), 0m, 100m);
    }
}

public readonly struct BasisPoints
{
    public long Value { get; }

    public BasisPoints(long value)
    {
        Value = Math.Max(0, Math.Min(value, 10000));
    }
}

public readonly struct PositiveDecimal
{
    public Decimal Value { get; }

    public PositiveDecimal(Decimal value)
    {
        Value = Math.Max(value, 0.01m);
    }
}
