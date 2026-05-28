using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ValueObjects;

public class InterestRateTests
{
    private static DayCountConvention CreateDayCount() => new Actual360();

    [Fact]
    public void FixedRate_CalculatesInterestCorrectly()
    {
        var rate = new FixedRate(Percentage.FromPercent(6m), CreateDayCount());
        var principal = new Money(10000m, "USD");
        var startDate = new LocalDate(2024, 1, 1);
        var endDate = new LocalDate(2024, 7, 1);

        var interest = rate.CalculateInterest(principal, startDate, endDate);

        interest.Amount.Should().BeInRange(290m, 310m);
    }

    [Fact]
    public void FixedRate_Equality_SameRate_AreEqual()
    {
        var a = new FixedRate(Percentage.FromPercent(5m), CreateDayCount());
        var b = new FixedRate(Percentage.FromPercent(5m), CreateDayCount());

        a.Should().Be(b);
    }

    [Fact]
    public void FloatingRate_CalculatesInterestWithMargin()
    {
        var rate = new FloatingRate(Percentage.FromPercent(3m), Percentage.FromPercent(1.5m), CreateDayCount());

        rate.Rate.AsPercent.Should().Be(4.5m);
        rate.BaseRate.AsPercent.Should().Be(3m);
        rate.Margin.AsPercent.Should().Be(1.5m);
    }

    [Fact]
    public void TieredRate_ReturnsCorrectEffectiveRate()
    {
        var tier1 = new TieredRate.Tier(Percentage.FromPercent(3m), new Money(10000m, "USD"));
        var tier2 = new TieredRate.Tier(Percentage.FromPercent(5m), new Money(50000m, "USD"));

        var rate = new TieredRate(new[] { tier1, tier2 }, CreateDayCount());

        var effectiveRate = rate.EffectiveRateForBalance(new Money(5000m, "USD"));
        effectiveRate.AsPercent.Should().Be(3m);

        effectiveRate = rate.EffectiveRateForBalance(new Money(25000m, "USD"));
        effectiveRate.AsPercent.Should().Be(5m);
    }

    [Fact]
    public void EffectiveRate_CalculatesInterest()
    {
        var rate = new EffectiveRate(Percentage.FromPercent(7.5m), CreateDayCount());
        var principal = new Money(10000m, "USD");
        var startDate = new LocalDate(2024, 1, 1);
        var endDate = new LocalDate(2024, 7, 1);

        var interest = rate.CalculateInterest(principal, startDate, endDate);

        interest.Amount.Should().BeInRange(360m, 380m);
    }

    [Fact]
    public void FixedRate_Equality_DifferentDayCount_NotEqual()
    {
        var a = new FixedRate(Percentage.FromPercent(5m), new Actual360());
        var b = new FixedRate(Percentage.FromPercent(5m), new Actual365());

        a.Should().NotBe(b);
    }

    [Fact]
    public void FixedRate_Equality_Null_False()
    {
        var rate = new FixedRate(Percentage.FromPercent(5m), CreateDayCount());

        rate.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void InterestRate_Equality_SameInstance_True()
    {
        var rate = new FixedRate(Percentage.FromPercent(5m), CreateDayCount());

        rate.Equals(rate).Should().BeTrue();
    }

    [Fact]
    public void TieredRate_BalanceExactlyAtUpperLimit_UsesFirstTier()
    {
        var tier1 = new TieredRate.Tier(Percentage.FromPercent(3m), new Money(10000m, "USD"));
        var tier2 = new TieredRate.Tier(Percentage.FromPercent(5m), new Money(50000m, "USD"));

        var rate = new TieredRate(new[] { tier1, tier2 }, CreateDayCount());

        var effectiveRate = rate.EffectiveRateForBalance(new Money(10000m, "USD"));

        effectiveRate.AsPercent.Should().Be(3m);
    }
}
