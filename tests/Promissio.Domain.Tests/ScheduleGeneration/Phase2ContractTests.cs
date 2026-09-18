using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ScheduleGeneration;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ScheduleGeneration;

/// <summary>
/// Synthetic contract regressions, independently worked in docs/domain/payment-schedules.md.
/// Interest recurrence uses the existing ISDA-referenced 30E/360 implementation.
/// These cases implement the owner-approved contracts in ADR-0004, not official EU schedules.
/// </summary>
public sealed class Phase2ContractTests
{
    private static readonly LocalDate Start = new(2024, 1, 1);
    private static readonly FixedRate Rate = new(new Percentage(0.12m), DayCountConventions.Thirty360European);

    private static IScheduleGenerator Generator(int kind) => kind switch
    {
        0 => new AnnuityScheduleGenerator(new InterestCalculator()),
        1 => new DifferentiatedScheduleGenerator(new InterestCalculator()),
        _ => new BulletScheduleGenerator(new InterestCalculator())
    };

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ThreePeriodSchedule_MatchesWorkedPrincipalAndInterest(int kind)
    {
        PaymentScheduleItem[] schedule = Generator(kind).Generate(new Money(1200, "EUR"), Rate, 3, Start).ToArray();
        decimal[] principal = kind switch
        {
            0 => [396.03m, 399.99m, 403.98m],
            1 => [400m, 400m, 400m],
            _ => [0m, 0m, 1200m]
        };
        decimal[] interest = kind switch
        {
            0 => [12m, 8.04m, 4.04m],
            1 => [12m, 8m, 4m],
            _ => [12m, 12m, 12m]
        };
        schedule.Select(p => p.PrincipalPortion.Amount).Should().Equal(principal);
        schedule.Select(p => p.InterestPortion.Amount).Should().Equal(interest);
    }

    [Theory]
    [InlineData(1, "1010")]
    [InlineData(2, "507.51")]
    public void Annuity_MatchesShortWorkedCases(int months, string payment)
    {
        PaymentScheduleItem[] schedule = Generator(0).Generate(new Money(1000, "EUR"), Rate, months, Start).ToArray();
        schedule[0].TotalPayment.Amount.Should().Be(decimal.Parse(payment, System.Globalization.CultureInfo.InvariantCulture));
        schedule.Sum(p => p.PrincipalPortion.Amount).Should().Be(1000);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Calendar_MovesPaymentButNotAccrualOrLaterContractualDates(int kind)
    {
        LocalDate start = new(2024, 5, 1);
        HolidayCalendar calendar = new([new LocalDate(2024, 6, 3)]);
        PaymentScheduleItem[] original = Generator(kind).Generate(new Money(1200, "EUR"), Rate, 3, start).ToArray();
        PaymentScheduleItem[] moved = Generator(kind).Generate(new Money(1200, "EUR"), Rate, 3, start,
            holidayCalendar: calendar).ToArray();
        moved[0].PaymentDate.Should().Be(new LocalDate(2024, 6, 4));
        moved[1].PaymentDate.Should().Be(new LocalDate(2024, 7, 1));
        moved.Select(p => p.ContractualDate).Should().Equal(original.Select(p => p.PaymentDate));
        moved.Select(p => p.InterestPortion).Should().Equal(original.Select(p => p.InterestPortion));
        moved.Select(p => p.PrincipalPortion).Should().Equal(original.Select(p => p.PrincipalPortion));
    }

    [Theory]
    [InlineData(0, 15, "5.6")]
    [InlineData(1, 15, "5.6")]
    [InlineData(2, 15, "5.6")]
    [InlineData(0, 61, "24")]
    [InlineData(1, 61, "24")]
    [InlineData(2, 61, "24")]
    public void ShortAndLongFirstPeriods_UseContractualAccrual(int kind, int firstOffset, string expectedInterest)
    {
        // January 1 -> January 15 (14/360), or March 1 (60/360).
        LocalDate first = firstOffset == 15 ? new LocalDate(2024, 1, 15) : new LocalDate(2024, 3, 1);
        PaymentScheduleItem[] schedule = Generator(kind).Generate(new Money(1200, "EUR"), Rate, 3, Start,
            firstPaymentDate: first).ToArray();
        schedule[0].PaymentDate.Should().Be(first);
        schedule[1].PaymentDate.Should().Be(first.PlusMonths(1));
        schedule[0].InterestPortion.Amount.Should().Be(decimal.Parse(expectedInterest, System.Globalization.CultureInfo.InvariantCulture));
        schedule.Sum(p => p.PrincipalPortion.Amount).Should().Be(1200);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MonthEnd_DoesNotDriftAfterFebruary(int kind)
    {
        PaymentScheduleItem[] schedule = Generator(kind).Generate(new Money(1200, "EUR"), Rate, 3,
            new LocalDate(2024, 1, 31)).ToArray();
        schedule.Select(p => p.PaymentDate).Should().Equal(
            new LocalDate(2024, 2, 29), new LocalDate(2024, 3, 31), new LocalDate(2024, 4, 30));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TinyPrincipal_LongTerm_IsNeverOverpaid(int kind)
    {
        PaymentScheduleItem[] schedule = Generator(kind).Generate(new Money(0.01m, "EUR"), Rate, 360, Start).ToArray();
        schedule.Sum(p => p.PrincipalPortion.Amount).Should().Be(0.01m);
        schedule.Should().OnlyContain(p => p.PrincipalPortion.Amount >= 0);
    }

    [Theory]
    [InlineData("EUR", "USD", "EUR")]
    [InlineData("EUR", "EUR", "USD")]
    public void PaymentItem_RejectsMixedCurrencies(string principal, string interest, string total)
    {
        Action create = () => new PaymentScheduleItem(1, Start, new Money(100, principal),
            new Money(10, interest), new Money(110, total));
        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Custom_PreservesIrregularCashFlowsAndCopiesInput()
    {
        List<CustomScheduleGenerator.CustomCashFlow> flows =
        [
            new(new Money(300, "EUR"), new Money(17, "EUR"), Start.PlusDays(10)),
            new(new Money(900, "EUR"), new Money(9, "EUR"), Start.PlusDays(75))
        ];
        CustomScheduleGenerator generator = new(flows, new InterestCalculator());
        flows.Clear();
        PaymentScheduleItem[] schedule = generator.Generate(new Money(1200, "EUR"), Rate, 2, Start).ToArray();
        schedule.Select(p => p.PrincipalPortion.Amount).Should().Equal(300m, 900m);
        schedule.Select(p => p.InterestPortion.Amount).Should().Equal(17m, 9m);
        schedule.Select(p => p.PaymentDate).Should().Equal(Start.PlusDays(10), Start.PlusDays(75));
    }

    [Fact]
    public void Custom_ValidatesGraceWithoutRewritingAmounts()
    {
        List<CustomScheduleGenerator.CustomCashFlow> flows =
        [
            new(Money.Zero("EUR"), new Money(17, "EUR")),
            new(new Money(1200, "EUR"), new Money(9, "EUR"))
        ];
        PaymentScheduleItem[] schedule = new CustomScheduleGenerator(flows, new InterestCalculator())
            .Generate(new Money(1200, "EUR"), Rate, 2, Start, gracePeriodMonths: 1).ToArray();
        schedule.Select(p => p.TotalPayment.Amount).Should().Equal(17m, 1209m);
    }

    [Theory]
    [InlineData("balance")]
    [InlineData("currency")]
    [InlineData("negative")]
    [InlineData("date")]
    [InlineData("count")]
    [InlineData("grace")]
    [InlineData("holiday")]
    public void Custom_RejectsInvalidFlows(string failure)
    {
        List<CustomScheduleGenerator.CustomCashFlow> flows =
        [
            new(new Money(300, failure == "currency" ? "USD" : "EUR"), new Money(failure == "negative" ? -1 : 17, "EUR"),
                failure == "date" ? Start : failure == "holiday" ? new LocalDate(2024, 1, 6) : Start.PlusDays(10)),
            new(new Money(failure == "balance" ? 800 : 900, "EUR"), new Money(9, "EUR"), Start.PlusDays(75))
        ];
        CustomScheduleGenerator generator = new(flows, new InterestCalculator());
        Action generate = () => generator.Generate(new Money(1200, "EUR"), Rate, failure == "count" ? 3 : 2,
            Start, gracePeriodMonths: failure == "grace" ? 1 : 0,
            holidayCalendar: failure == "holiday" ? new HolidayCalendar([]) : null);
        generate.Should().Throw<ArgumentException>();
    }
}
