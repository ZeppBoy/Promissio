using FluentAssertions;
using NodaTime;
using Promissio.Domain.ScheduleGeneration;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ScheduleGeneration;

/// <summary>
/// Time intervals from SWD(2012)128 section 4.1.1, printed pages 23–26:
/// https://www.mfcr.cz/assets/attachments/EU-MFCR_Metodika_2012_128-Guidelines-consumer-credit-directive-swd-en.pdf
/// Cash amounts are synthetic; the single-payment solution is independently invertible.
/// </summary>
public sealed class AprcDatedTests
{
    [Theory]
    [InlineData(2012, 1, 12, 2012, 2, 15, 1, 3, 365)]
    [InlineData(2012, 1, 12, 2012, 3, 15, 2, 3, 365)]
    [InlineData(2012, 1, 12, 2012, 4, 15, 3, 3, 365)]
    [InlineData(2013, 1, 12, 2013, 2, 15, 1, 3, 366)]
    [InlineData(2013, 1, 12, 2013, 3, 15, 2, 3, 366)]
    [InlineData(2013, 1, 12, 2013, 4, 15, 3, 3, 366)]
    [InlineData(2013, 2, 25, 2013, 3, 28, 1, 3, 366)]
    [InlineData(2013, 2, 26, 2013, 3, 29, 1, 2, 366)]
    [InlineData(2012, 2, 26, 2012, 3, 29, 1, 3, 366)]
    [InlineData(2012, 12, 1, 2013, 2, 2, 2, 1, 366)]
    public void PublishedMonthlyIntervals_AreUsedInsteadOfPeriodNumbers(
        int sy, int sm, int sd, int ey, int em, int ed, int months, int days, int denominator)
    {
        LocalDate start = new(sy, sm, sd);
        LocalDate end = new(ey, em, ed);
        double years = months / 12d + (double)days / denominator;
        decimal expected = (decimal)(Math.Pow(1.1d, 1d / years) - 1d);
        Percentage actual = new AprcCalculator().Calculate(new Money(1000, "EUR"), [Payment(end, 1100, 73)], start);
        actual.Fraction.Should().BeApproximately(expected, 0.000000001m);
    }

    [Theory]
    [InlineData(2012, 0)]
    [InlineData(2013, 1)]
    [InlineData(2014, 2)]
    public void PublishedAnnualIntervals_RespectSelectedFrequency(int paymentYear, int wholeYears)
    {
        LocalDate start = new(2012, 1, 12);
        LocalDate end = new(paymentYear, 2, 15);
        decimal expected = (decimal)(Math.Pow(1.1, 1d / (wholeYears + 34d / 365d)) - 1d);
        new AprcCalculator(AprcPeriodUnit.Years).Calculate(new Money(1000, "EUR"), [Payment(end, 1100)], start)
            .Fraction.Should().BeApproximately(expected, 0.000000001m);
    }

    [Fact]
    public void WeeklyFrequency_UsesEqualWeeks()
    {
        LocalDate start = new(2024, 1, 1);
        new AprcCalculator(AprcPeriodUnit.Weeks).Calculate(new Money(1000, "EUR"),
            [Payment(start.PlusWeeks(52), 1100)], start).Fraction.Should().BeApproximately(0.1m, 0.000000001m);
    }

    [Fact]
    public void MovingDates_ChangesAprcButRelabellingPeriodsDoesNot()
    {
        LocalDate start = new(2024, 1, 1);
        AprcCalculator calculator = new();
        Money principal = new(1000, "EUR");
        Percentage original = calculator.Calculate(principal, [Payment(start.PlusYears(1), 1100)], start);
        calculator.Calculate(principal, [Payment(start.PlusYears(1), 1100, 99)], start).Should().Be(original);
        calculator.Calculate(principal, [Payment(start.PlusYears(2), 1100)], start).Fraction
            .Should().BeApproximately(0.04880884817m, 0.000000001m);
        calculator.Calculate(principal, [Payment(start.PlusYears(1), 1100)], start.PlusMonths(1)).Fraction
            .Should().BeGreaterThan(original.Fraction);
    }

    [Fact]
    public void UpfrontFee_IsEquivalentToNetDrawdown()
    {
        LocalDate start = new(2024, 1, 1);
        AprcCalculator calculator = new();
        Percentage net = calculator.Calculate(new Money(940, "EUR"), [Payment(start.PlusYears(1), 1100)], start);
        Percentage gross = calculator.Calculate(new Money(1000, "EUR"),
            [Payment(start, 60), Payment(start.PlusYears(1), 1100, 2)], start);
        gross.Fraction.Should().BeApproximately(net.Fraction, 0.000000001m);
    }

    [Fact]
    public void ZeroCost_ReturnsExactZero()
    {
        LocalDate start = new(2024, 1, 1);
        new AprcCalculator().Calculate(new Money(1000, "EUR"), [Payment(start.PlusYears(1), 1000)], start)
            .Fraction.Should().Be(0m);
    }

    [Fact]
    public void LargeAnnualRate_ExpandsBracket()
    {
        LocalDate start = new(2024, 1, 1);
        new AprcCalculator().Calculate(new Money(100, "EUR"), [Payment(start.PlusYears(1), 1100)], start)
            .Fraction.Should().BeApproximately(10m, 0.000000001m);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("principal")]
    [InlineData("currency")]
    [InlineData("past")]
    [InlineData("immediate")]
    [InlineData("negative-rate")]
    [InlineData("no-finite-root")]
    [InlineData("iterations")]
    [InlineData("non-convergence")]
    public void InvalidOrUnsolvableInputs_ReturnFailureWithoutThrowing(string failure)
    {
        LocalDate start = new(2024, 1, 1);
        Money principal = new(failure == "principal" ? 0 : 1000, "EUR");
        PaymentScheduleItem[] schedule = failure switch
        {
            "empty" => [],
            "currency" => [new PaymentScheduleItem(1, start.PlusYears(1), new Money(1100, "USD"), Money.Zero("USD"), new Money(1100, "USD"))],
            "past" => [Payment(start.PlusDays(-1), 1100)],
            "immediate" => [Payment(start, 1100)],
            "negative-rate" => [Payment(start.PlusYears(1), 900)],
            "no-finite-root" => [Payment(start, 1000), Payment(start.PlusYears(1), 100)],
            _ => [Payment(start.PlusYears(1), 1100)]
        };
        int iterations = failure == "iterations" ? 0 : failure == "non-convergence" ? 1 : 100;
        AprcCalculationResult result = new AprcCalculator().TryCalculate(principal, schedule, start, iterations);
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }

    private static PaymentScheduleItem Payment(LocalDate date, decimal amount, int period = 1) =>
        new(period, date, new Money(amount, "EUR"), Money.Zero("EUR"), new Money(amount, "EUR"));
}
