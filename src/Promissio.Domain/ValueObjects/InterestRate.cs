using NodaTime;
using Promissio.Domain.Calculations.DayCounts;

namespace Promissio.Domain.ValueObjects;

/// <summary>
/// Abstract base for interest rate representations.
/// </summary>
public abstract class InterestRate : IEquatable<InterestRate>
{
    public abstract Percentage Rate { get; }

    public abstract Money CalculateInterest(Money principal, LocalDate startDate, LocalDate endDate);

    public bool Equals(InterestRate? other) => other != null && this.GetType() == other.GetType() && this.Rate == other.Rate;

    public static bool operator ==(InterestRate? left, InterestRate? right) => Equals(left, right);

    public static bool operator !=(InterestRate? left, InterestRate? right) => !Equals(left, right);

    public override bool Equals(object? obj) => Equals(obj as InterestRate);

    public override int GetHashCode() => HashCode.Combine(GetType(), Rate);
}

/// <summary>
/// Fixed interest rate that does not change over the life of the loan.
/// </summary>
public sealed class FixedRate(Percentage rate, DayCountConvention dayCountConvention) : InterestRate
{
    public override Percentage Rate { get; } = rate;

    public DayCountConvention DayCountConvention { get; } = dayCountConvention;

    public override Money CalculateInterest(Money principal, LocalDate startDate, LocalDate endDate)
    {
        Decimal dayCountFraction = DayCountConvention.Fraction(startDate, endDate);
        return principal * (Rate.Fraction * dayCountFraction);
    }

    public override string ToString() => $"FixedRate({Rate}, {DayCountConvention})";
}

/// <summary>
/// Floating interest rate based on a reference rate plus a fixed margin.
/// </summary>
public sealed class FloatingRate(Percentage baseRate, Percentage margin, DayCountConvention dayCountConvention) : InterestRate
{
    public override Percentage Rate { get; } = baseRate + margin;

    public Percentage BaseRate { get; } = baseRate;

    public Percentage Margin { get; } = margin;

    public DayCountConvention DayCountConvention { get; } = dayCountConvention;

    public override Money CalculateInterest(Money principal, LocalDate startDate, LocalDate endDate)
    {
        Decimal dayCountFraction = DayCountConvention.Fraction(startDate, endDate);
        return principal * (Rate.Fraction * dayCountFraction);
    }

    public override string ToString() => $"FloatingRate(base={BaseRate}, margin={Margin}, total={Rate})";
}

/// <summary>
/// Tiered interest rate that applies different rates based on balance bands or time periods.
/// </summary>
public sealed class TieredRate(IList<TieredRate.Tier> tiers, DayCountConvention dayCountConvention) : InterestRate
{
    public IList<TieredRate.Tier> Tiers { get; } = tiers;

    public DayCountConvention DayCountConvention { get; } = dayCountConvention;

    /// <summary>
    /// Returns the effective rate based on the current balance.
    /// </summary>
    public Percentage EffectiveRateForBalance(Money balance)
    {
        if (Tiers.Count == 0)
            throw new InvalidOperationException("TieredRate must have at least one tier.");

        Tier? activeTier = Tiers.FirstOrDefault(t => balance <= t.UpperLimit) ?? Tiers[^1];
        return activeTier.Rate;
    }

    public override Percentage Rate => Tiers[^1].Rate;

    public override Money CalculateInterest(Money principal, LocalDate startDate, LocalDate endDate)
    {
        Percentage effectiveRate = EffectiveRateForBalance(principal);
        Decimal dayCountFraction = DayCountConvention.Fraction(startDate, endDate);
        return principal * (effectiveRate.Fraction * dayCountFraction);
    }

    public override string ToString() => $"TieredRate(tiers: {string.Join(", ", Tiers)})";

    /// <summary>
    /// A single tier with an upper balance limit and the rate that applies within that tier.
    /// </summary>
    public sealed class Tier(Percentage rate, Money upperLimit) : IEquatable<Tier>
    {
        public Percentage Rate { get; } = rate;

        public Money UpperLimit { get; } = upperLimit;

        public bool Equals(Tier? other) => other != null && this.Rate == other.Rate && this.UpperLimit == other.UpperLimit;

        public override bool Equals(object? obj) => Equals(obj as Tier);

        public override int GetHashCode() => HashCode.Combine(Rate, UpperLimit);

        public override string ToString() => $"Tier(rate={Rate}, limit={UpperLimit})";
    }
}

/// <summary>
/// Effective interest rate representing the APRC (Annual Percentage Rate of Charge).
/// Calculated using an iterative solver per EU Consumer Credit Directive.
/// </summary>
public sealed class EffectiveRate(Percentage rate, DayCountConvention dayCountConvention) : InterestRate
{
    public override Percentage Rate { get; } = rate;

    public DayCountConvention DayCountConvention { get; } = dayCountConvention;

    public override Money CalculateInterest(Money principal, LocalDate startDate, LocalDate endDate)
    {
        Decimal dayCountFraction = DayCountConvention.Fraction(startDate, endDate);
        return principal * (Rate.Fraction * dayCountFraction);
    }

    public override string ToString() => $"EffectiveRate({Rate})";
}
