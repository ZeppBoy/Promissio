using System.Linq;
using NodaTime;
using Promissio.Domain.Calculations.DayCounts;

namespace Promissio.Domain.ValueObjects;

/// <summary>
/// Abstract base for interest rate representations.
/// </summary>
public abstract record InterestRate
{
    public abstract Percentage Rate { get; }

    public abstract Money CalculateInterest(Money principal, LocalDate startDate, LocalDate endDate);
}

/// <summary>
/// Fixed interest rate that does not change over the life of the loan.
/// </summary>
public sealed record FixedRate : InterestRate
{
    public override Percentage Rate { get; }

    public DayCountConvention DayCountConvention { get; init; }

    public FixedRate(Percentage rate, DayCountConvention dayCountConvention)
    {
        Rate = rate;
        DayCountConvention = dayCountConvention;
    }

    public override Money CalculateInterest(Money principal, LocalDate startDate, LocalDate endDate)
    {
        Decimal dayCountFraction = DayCountConvention.Fraction(startDate, endDate);
        return principal * (Rate.Fraction * dayCountFraction);
    }

    public bool Equals(FixedRate? other) =>
        other is not null
        && Rate == other.Rate
        && DayCountConvention.Equals(other.DayCountConvention);

    public override int GetHashCode() => HashCode.Combine(Rate, DayCountConvention);

    public override string ToString() => $"FixedRate({Rate}, {DayCountConvention})";
}

/// <summary>
/// Floating interest rate based on a reference rate plus a fixed margin.
/// </summary>
/// <remarks>
/// Re-pricing logic (reset schedule for base rate updates) is planned for a future phase.
/// See developers_plan.md for the roadmap.
/// </remarks>
public sealed record FloatingRate : InterestRate
{
    public Percentage BaseRate { get; init; }

    public Percentage Margin { get; init; }

    public DayCountConvention DayCountConvention { get; init; }

    public FloatingRate(Percentage baseRate, Percentage margin, DayCountConvention dayCountConvention)
    {
        BaseRate = baseRate;
        Margin = margin;
        DayCountConvention = dayCountConvention;
    }

    public override Percentage Rate => BaseRate + Margin;

    public override Money CalculateInterest(Money principal, LocalDate startDate, LocalDate endDate)
    {
        Decimal dayCountFraction = DayCountConvention.Fraction(startDate, endDate);
        return principal * (Rate.Fraction * dayCountFraction);
    }

    public bool Equals(FloatingRate? other) =>
        other is not null
        && BaseRate == other.BaseRate
        && Margin == other.Margin
        && DayCountConvention.Equals(other.DayCountConvention);

    public override int GetHashCode() => HashCode.Combine(BaseRate, Margin, DayCountConvention);

    public override string ToString() => $"FloatingRate(base={BaseRate}, margin={Margin}, total={Rate})";
}

/// <summary>
/// Tiered interest rate that applies different rates based on balance bands or time periods.
/// </summary>
public sealed record TieredRate : InterestRate
{
    public IReadOnlyList<Tier> Tiers { get; }

    public DayCountConvention DayCountConvention { get; init; }

    public TieredRate(IReadOnlyList<Tier> tiers, DayCountConvention dayCountConvention)
    {
        Tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
        DayCountConvention = dayCountConvention;
    }

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

    public bool Equals(TieredRate? other) =>
        other is not null
        && DayCountConvention.Equals(other.DayCountConvention)
        && Tiers.SequenceEqual(other.Tiers);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var tier in Tiers)
            hash.Add(tier);
        hash.Add(DayCountConvention);
        return hash.ToHashCode();
    }

    public override string ToString() => $"TieredRate(tiers: {string.Join(", ", Tiers)})";

    /// <summary>
    /// A single tier with an upper balance limit and the rate that applies within that tier.
    /// </summary>
    public sealed record Tier(Percentage Rate, Money UpperLimit)
    {
        public override string ToString() => $"Tier(rate={Rate}, limit={UpperLimit})";
    }
}

/// <summary>
/// Effective interest rate representing the APRC (Annual Percentage Rate of Charge).
/// Calculated using an iterative solver per EU Consumer Credit Directive.
/// </summary>
public sealed record EffectiveRate : InterestRate
{
    public override Percentage Rate { get; }

    public DayCountConvention DayCountConvention { get; init; }

    public EffectiveRate(Percentage rate, DayCountConvention dayCountConvention)
    {
        Rate = rate;
        DayCountConvention = dayCountConvention;
    }

    public override Money CalculateInterest(Money principal, LocalDate startDate, LocalDate endDate)
    {
        Decimal dayCountFraction = DayCountConvention.Fraction(startDate, endDate);
        return principal * (Rate.Fraction * dayCountFraction);
    }

    public bool Equals(EffectiveRate? other) =>
        other is not null
        && Rate == other.Rate
        && DayCountConvention.Equals(other.DayCountConvention);

    public override int GetHashCode() => HashCode.Combine(Rate, DayCountConvention);

    public override string ToString() => $"EffectiveRate({Rate})";
}
