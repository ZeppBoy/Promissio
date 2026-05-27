using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.Calculations;

public class InterestCalculatorTests
{
    private readonly IInterestCalculator _calculator;

    public InterestCalculatorTests()
    {
        _calculator = new InterestCalculator();
    }

    #region Basic single-period calculations

    [Fact]
    public void Calculate_FixedRate_Actual360_30Days_ReturnsCorrectInterest()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual360());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 1, 31);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = 10000m * 0.05m * (30m / 360m);
        expected = Math.Round(expected, 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "USD"));
    }

    [Fact]
    public void Calculate_FixedRate_Actual365_OneYear_NonLeap_ReturnsAnnualInterest()
    {
        var principal = new Money(50000m, "GBP");
        var rate = new FixedRate(Percentage.FromPercent(4.5m), new Actual365());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2024, 1, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = 50000m * 0.045m * (365m / 365m);
        expected = Math.Round(expected, 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "GBP"));
    }

    [Fact]
    public void Calculate_FixedRate_ActualActual_SameDay_ReturnsZero()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(6m), new ActualActual());
        var date = new LocalDate(2023, 6, 15);

        Money interest = _calculator.Calculate(principal, rate, date, date);

        interest.Should().Be(Money.Zero("USD"));
    }

    [Fact]
    public void Calculate_FixedRate_Thirty360_OneMonth_ReturnsCorrectInterest()
    {
        var principal = new Money(100000m, "EUR");
        var rate = new FixedRate(Percentage.FromPercent(3m), new Thirty360());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 2, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = 100000m * 0.03m * (30m / 360m);
        expected = Math.Round(expected, 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "EUR"));
    }

    [Fact]
    public void Calculate_FloatingRate_Actual360_ReturnsCorrectInterest()
    {
        var principal = new Money(25000m, "USD");
        var baseRate = Percentage.FromPercent(2.5m);
        var margin = Percentage.FromPercent(1m);
        var rate = new FloatingRate(baseRate, margin, new Actual360());
        var startDate = new LocalDate(2023, 3, 1);
        var endDate = new LocalDate(2023, 4, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = 25000m * 0.035m * (31m / 360m);
        expected = Math.Round(expected, 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "USD"));
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Calculate_SameDate_ReturnsZero()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual360());
        var date = new LocalDate(2023, 7, 1);

        Money interest = _calculator.Calculate(principal, rate, date, date);

        interest.Should().Be(Money.Zero("USD"));
    }

    [Fact]
    public void Calculate_StartDateAfterEndDate_ThrowsArgumentOutOfRangeException()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual360());
        var startDate = new LocalDate(2023, 7, 1);
        var endDate = new LocalDate(2023, 6, 1);

        Action act = () => _calculator.Calculate(principal, rate, startDate, endDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_ZeroPrincipal_ReturnsZero()
    {
        var principal = Money.Zero("USD");
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual360());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 2, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        interest.Should().Be(Money.Zero("USD"));
    }

    [Fact]
    public void Calculate_ZeroRate_ReturnsZero()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(0m), new Actual360());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 2, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        interest.Should().Be(Money.Zero("USD"));
    }

    [Fact]
    public void Calculate_CrossesLeapYear_Actual365_ReturnsCorrectInterest()
    {
        var principal = new Money(100000m, "EUR");
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual365());
        var startDate = new LocalDate(2024, 2, 28);
        var endDate = new LocalDate(2024, 3, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = 100000m * 0.05m * (2m / 365m);
        expected = Math.Round(expected, 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "EUR"));
    }

    [Fact]
    public void Calculate_CrossesYearBoundary_ActualActual_ReturnsCorrectInterest()
    {
        var principal = new Money(50000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(4m), new ActualActual());
        var startDate = new LocalDate(2023, 12, 31);
        var endDate = new LocalDate(2024, 1, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = 50000m * 0.04m * (1m / 365m);
        expected = Math.Round(expected, 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "USD"));
    }

    #endregion

    #region Different conventions comparison

    [Fact]
    public void Calculate_SamePeriod_DifferentConventions_ProducesDifferentResults()
    {
        var principal = new Money(100000m, "USD");
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2024, 1, 1);

        Money actual360 = _calculator.Calculate(
            principal,
            new FixedRate(Percentage.FromPercent(5m), new Actual360()),
            startDate, endDate);

        Money actual365 = _calculator.Calculate(
            principal,
            new FixedRate(Percentage.FromPercent(5m), new Actual365()),
            startDate, endDate);

        Money actualActual = _calculator.Calculate(
            principal,
            new FixedRate(Percentage.FromPercent(5m), new ActualActual()),
            startDate, endDate);

        Money thirty360 = _calculator.Calculate(
            principal,
            new FixedRate(Percentage.FromPercent(5m), new Thirty360()),
            startDate, endDate);

        actual360.Amount.Should().BeGreaterThan(actual365.Amount);
        actual360.Amount.Should().BeGreaterThan(thirty360.Amount);
        actual365.Amount.Should().Be(actualActual.Amount);
    }

    #endregion

    #region Multi-period calculations

    [Fact]
    public void CalculateForPeriods_ReturnsCorrectNumberOfResults()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual360());
        var periods = new List<(LocalDate StartDate, LocalDate EndDate)>
        {
            (new LocalDate(2023, 1, 1), new LocalDate(2023, 2, 1)),
            (new LocalDate(2023, 2, 1), new LocalDate(2023, 3, 1)),
            (new LocalDate(2023, 3, 1), new LocalDate(2023, 4, 1))
        };

        IReadOnlyList<Money> results = _calculator.CalculateForPeriods(principal, rate, periods);

        results.Count.Should().Be(3);
    }

    [Fact]
    public void CalculateForPeriods_EachPeriodHasCorrectInterest()
    {
        var principal = new Money(12000m, "EUR");
        var rate = new FixedRate(Percentage.FromPercent(6m), new Actual360());
        var periods = new List<(LocalDate StartDate, LocalDate EndDate)>
        {
            (new LocalDate(2023, 1, 1), new LocalDate(2023, 2, 1)),
            (new LocalDate(2023, 2, 1), new LocalDate(2023, 3, 1))
        };

        IReadOnlyList<Money> results = _calculator.CalculateForPeriods(principal, rate, periods);

        Decimal janExpected = Math.Round(12000m * 0.06m * (31m / 360m), 2, MidpointRounding.ToEven);
        Decimal febExpected = Math.Round(12000m * 0.06m * (28m / 360m), 2, MidpointRounding.ToEven);

        results[0].Should().Be(new Money(janExpected, "EUR"));
        results[1].Should().Be(new Money(febExpected, "EUR"));
    }

    [Fact]
    public void CalculateForPeriods_EmptyPeriods_ReturnsEmptyList()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual360());
        var periods = new List<(LocalDate StartDate, LocalDate EndDate)>();

        IReadOnlyList<Money> results = _calculator.CalculateForPeriods(principal, rate, periods);

        results.Should().BeEmpty();
    }

    [Fact]
    public void CalculateForPeriods_AllSameDayPeriods_ReturnZeros()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual360());
        var periods = new List<(LocalDate StartDate, LocalDate EndDate)>
        {
            (new LocalDate(2023, 1, 1), new LocalDate(2023, 1, 1)),
            (new LocalDate(2023, 2, 1), new LocalDate(2023, 2, 1))
        };

        IReadOnlyList<Money> results = _calculator.CalculateForPeriods(principal, rate, periods);

        results.Should().HaveCount(2);
        foreach (var interest in results)
        {
            interest.Should().Be(Money.Zero("USD"));
        }
    }

    #endregion

    #region TieredRate

    [Fact]
    public void Calculate_TieredRate_BalanceInFirstTier_UsesFirstTierRate()
    {
        var principal = new Money(5000m, "USD");
        var tier1 = new TieredRate.Tier(Percentage.FromPercent(3m), new Money(10000m, "USD"));
        var tier2 = new TieredRate.Tier(Percentage.FromPercent(5m), new Money(50000m, "USD"));
        var rate = new TieredRate(new[] { tier1, tier2 }.AsReadOnly(), new Actual360());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 2, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = Math.Round(5000m * 0.03m * (31m / 360m), 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "USD"));
    }

    [Fact]
    public void Calculate_TieredRate_BalanceAboveAllTiers_UsesHighestTierRate()
    {
        var principal = new Money(75000m, "USD");
        var tier1 = new TieredRate.Tier(Percentage.FromPercent(3m), new Money(10000m, "USD"));
        var tier2 = new TieredRate.Tier(Percentage.FromPercent(5m), new Money(50000m, "USD"));
        var rate = new TieredRate(new[] { tier1, tier2 }.AsReadOnly(), new Actual360());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 2, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = Math.Round(75000m * 0.05m * (31m / 360m), 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "USD"));
    }

    #endregion

    #region EffectiveRate

    [Fact]
    public void Calculate_EffectiveRate_ReturnsCorrectInterest()
    {
        var principal = new Money(100000m, "EUR");
        var rate = new EffectiveRate(Percentage.FromPercent(6.5m), new Actual360());
        var startDate = new LocalDate(2023, 6, 1);
        var endDate = new LocalDate(2023, 9, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = Math.Round(100000m * 0.065m * (92m / 360m), 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "EUR"));
    }

    #endregion

    #region Precision and rounding

    [Fact]
    public void Calculate_SmallInterest_RoundsToTwoDecimalPlaces()
    {
        var principal = new Money(100m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(0.5m), new Actual360());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 1, 2);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        interest.Amount.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture).Should().Match("*.*0", "Interest should be rounded to two decimal places");
    }

    [Fact]
    public void Calculate_LargePrincipal_MaintainsPrecision()
    {
        var principal = new Money(10000000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(7.25m), new Actual360());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 7, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = Math.Round(10000000m * 0.0725m * (181m / 360m), 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "USD"));
    }

    [Fact]
    public void Calculate_BankersRounding_HalfEven()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual360());
        var startDate = new LocalDate(2023, 1, 1);
        var endDate = new LocalDate(2023, 1, 4);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal raw = 10000m * 0.05m * (3m / 360m);
        Decimal expected = Math.Round(raw, 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "USD"));
    }

    #endregion

    #region Thirty360European

    [Fact]
    public void Calculate_Thirty360European_February_ReturnsCorrectInterest()
    {
        var principal = new Money(200000m, "EUR");
        var rate = new FixedRate(Percentage.FromPercent(2.5m), new Thirty360European());
        var startDate = new LocalDate(2024, 2, 1);
        var endDate = new LocalDate(2024, 3, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        Decimal expected = Math.Round(200000m * 0.025m * (30m / 360m), 2, MidpointRounding.ToEven);
        interest.Should().Be(new Money(expected, "EUR"));
    }

    [Fact]
    public void Calculate_ActualActual_MultiYearPeriod_ReturnsCorrectInterest()
    {
        var principal = new Money(10000m, "USD");
        var rate = new FixedRate(Percentage.FromPercent(5m), new ActualActual());
        var startDate = new LocalDate(2023, 6, 1);
        var endDate = new LocalDate(2025, 6, 1);

        Money interest = _calculator.Calculate(principal, rate, startDate, endDate);

        interest.Amount.Should().BeGreaterThan(0m);
        Decimal maxInterest = 10000m * 0.05m * 2m;
        interest.Amount.Should().BeLessOrEqualTo(maxInterest);
    }

    #endregion
}
