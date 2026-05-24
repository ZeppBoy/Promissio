using FsCheck;
using FsCheck.Xunit;
using NodaTime;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Tests.ValueObjects;

public class InterestRatePropertyTests
{
    private static DayCountConvention CreateDayCount() => new Actual360();

    [Property]
    public bool FixedRate_Equality_WithSameRate(SafePercent a)
    {
        var rateA = new FixedRate(Percentage.FromPercent(a.Value), CreateDayCount());
        var rateB = new FixedRate(Percentage.FromPercent(a.Value), CreateDayCount());

        return rateA == rateB;
    }

    [Property]
    public bool FixedRate_HashCode_ConsistentWithEquality(SafePercent a)
    {
        var rateA = new FixedRate(Percentage.FromPercent(a.Value), CreateDayCount());
        var rateB = new FixedRate(Percentage.FromPercent(a.Value), CreateDayCount());

        return rateA.GetHashCode() == rateB.GetHashCode();
    }

    [Property]
    public bool FloatingRate_RateEqualsBasePlusMargin(SafePercent baseRate, SafePercent margin)
    {
        var rate = new FloatingRate(Percentage.FromPercent(baseRate.Value), Percentage.FromPercent(margin.Value), CreateDayCount());

        return Math.Abs(rate.Rate.AsPercent - (baseRate.Value + margin.Value)) < 0.0001m;
    }

    [Property]
    public bool FixedRate_CalculatesZeroInterestForZeroPrincipal(InterestRateMonths months)
    {
        var rate = new FixedRate(Percentage.FromPercent(5m), CreateDayCount());
        var principal = Money.Zero("USD");
        var startDate = new LocalDate(2024, 1, 1);
        var endDate = startDate.PlusMonths(months.Value);

        var interest = rate.CalculateInterest(principal, startDate, endDate);

        return interest.Amount == 0m;
    }

    [Property]
    public bool TieredRate_EffectiveRate_WithinTier(PositiveBalance balance)
    {
        var tier1 = new TieredRate.Tier(Percentage.FromPercent(3m), new Money(10000m, "USD"));
        var tier2 = new TieredRate.Tier(Percentage.FromPercent(5m), new Money(50000m, "USD"));

        var rate = new TieredRate(new[] { tier1, tier2 }, CreateDayCount());

        var effectiveRate = rate.EffectiveRateForBalance(new Money(balance.Value, "USD"));

        return effectiveRate.AsPercent >= 3m && effectiveRate.AsPercent <= 5m;
    }
}

public readonly struct InterestRateMonths
{
    public int Value { get; }

    public InterestRateMonths(int value)
    {
        Value = Math.Max(1, value);
    }
}

public readonly struct PositiveBalance
{
    public Decimal Value { get; }

    public PositiveBalance(Decimal value)
    {
        Value = Math.Max(value, 1m);
    }
}
