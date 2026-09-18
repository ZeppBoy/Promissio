using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ScheduleGeneration;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ScheduleGeneration;

/// <summary>
/// Boundary and failure-contract checks for the owner-approved behavior in ADR-0004.
/// </summary>
public sealed class Phase2BoundaryTests
{
    private const string Currency = "EUR";
    private static readonly LocalDate Start = new(2024, 1, 1);
    private static readonly FixedRate Rate = new(
        Percentage.FromPercent(12m),
        DayCountConventions.Thirty360European);

    [Fact]
    public void ScheduleValidation_ReportsNonPositivePrincipal()
    {
        Action generate = () => Generator().Generate(
            Money.Zero(Currency), Rate, 12, Start).ToArray();

        generate.Should().Throw<ArgumentException>()
            .WithMessage("*Principal must be positive.*");
    }

    [Fact]
    public void ScheduleValidation_ReportsNonPositiveTerm()
    {
        Action generate = () => Generator().Generate(
            new Money(1_000m, Currency), Rate, 0, Start).ToArray();

        generate.Should().Throw<ArgumentException>()
            .WithMessage("*Term must be positive.*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(12)]
    public void ScheduleValidation_ReportsInvalidGrace(int graceMonths)
    {
        Action generate = () => Generator().Generate(
            new Money(1_000m, Currency), Rate, 12, Start, graceMonths).ToArray();

        generate.Should().Throw<ArgumentException>()
            .WithMessage("*Grace must be non-negative and shorter than the term.*");
    }

    [Fact]
    public void ScheduleValidation_RejectsFirstPaymentOnDisbursementDate()
    {
        Action generate = () => Generator().Generate(
            new Money(1_000m, Currency), Rate, 12, Start, firstPaymentDate: Start).ToArray();

        generate.Should().Throw<ArgumentException>()
            .WithMessage("*First payment must follow disbursement.*");
    }

    [Fact]
    public void PaymentItem_AcceptsExactlyOneCentReconciliationTolerance()
    {
        PaymentScheduleItem item = new(
            1,
            Start,
            new Money(10m, Currency),
            new Money(1m, Currency),
            new Money(11.01m, Currency));

        item.TotalPayment.Amount.Should().Be(11.01m);
    }

    [Fact]
    public void PaymentItem_ReportsReconciliationOutsideTolerance()
    {
        Action create = () => new PaymentScheduleItem(
            1,
            Start,
            new Money(10m, Currency),
            new Money(1m, Currency),
            new Money(11.02m, Currency));

        create.Should().Throw<ArgumentException>()
            .WithMessage("*Total payment*must equal PrincipalPortion + InterestPortion*");
    }

    [Theory]
    [InlineData("principal", "Principal portion must be non-negative.")]
    [InlineData("interest", "Interest portion must be non-negative.")]
    [InlineData("total", "Total payment must be non-negative.")]
    public void PaymentItem_ReportsNegativeComponent(string component, string message)
    {
        Money principal = new(component == "principal" ? -1m : 0m, Currency);
        Money interest = new(component == "interest" ? -1m : 0m, Currency);
        Money total = new(component == "total" ? -1m : principal.Amount + interest.Amount, Currency);

        Action create = () => new PaymentScheduleItem(1, Start, principal, interest, total);

        create.Should().Throw<ArgumentException>().WithMessage($"*{message}*");
    }

    [Fact]
    public void PaymentItem_DeconstructsAllPublishedComponents()
    {
        PaymentScheduleItem item = new(
            7,
            Start.PlusMonths(1),
            new Money(10m, Currency),
            new Money(1m, Currency),
            new Money(11m, Currency));

        (int period, LocalDate date, Money principal, Money interest, Money total) = item;

        period.Should().Be(7);
        date.Should().Be(Start.PlusMonths(1));
        principal.Amount.Should().Be(10m);
        interest.Amount.Should().Be(1m);
        total.Amount.Should().Be(11m);
    }

    [Theory]
    [InlineData("count", "Supply one cash flow per instalment.")]
    [InlineData("currency", "Cash-flow currencies must match the principal.")]
    [InlineData("grace", "Supplied grace instalments must be interest-only.")]
    [InlineData("date", "Cash-flow dates must increase strictly after disbursement.")]
    [InlineData("holiday", "An explicit payment date must be a business day in the supplied calendar.")]
    [InlineData("balance", "Supplied principal portions must repay the principal exactly.")]
    public void CustomSchedule_ReportsInvalidContract(string failure, string expectedMessage)
    {
        List<CustomScheduleGenerator.CustomCashFlow> flows =
        [
            new(
                new Money(failure == "balance" ? 400m : failure == "grace" ? 100m : 500m,
                    failure == "currency" ? "USD" : Currency),
                new Money(10m, Currency),
                failure == "date" ? Start : failure == "holiday" ? new LocalDate(2024, 1, 6) : Start.PlusDays(10)),
            new(new Money(500m, Currency), new Money(5m, Currency), Start.PlusDays(40))
        ];
        CustomScheduleGenerator generator = new(flows, new InterestCalculator());
        Action generate = () => generator.Generate(
            new Money(1_000m, Currency),
            Rate,
            failure == "count" ? 3 : 2,
            Start,
            failure == "grace" ? 1 : 0,
            failure == "holiday" ? new HolidayCalendar([]) : null).ToArray();

        generate.Should().Throw<ArgumentException>().WithMessage($"*{expectedMessage}*");
    }

    [Fact]
    public void CustomSchedule_UsesExplicitDateAsPaymentAndContractualDate()
    {
        LocalDate explicitDate = Start.PlusDays(10);
        List<CustomScheduleGenerator.CustomCashFlow> flows =
        [
            new(new Money(1_000m, Currency), new Money(10m, Currency), explicitDate)
        ];

        PaymentScheduleItem item = new CustomScheduleGenerator(flows, new InterestCalculator())
            .Generate(new Money(1_000m, Currency), Rate, 1, Start).Single();

        item.PaymentDate.Should().Be(explicitDate);
        item.ContractualDate.Should().Be(explicitDate);
    }

    [Fact]
    public void HolidayCalendar_NearestBusinessDayMovesForwardOnTie()
    {
        LocalDate holiday = new(2024, 6, 5);
        HolidayCalendar calendar = new([holiday]);

        calendar.NearestBusinessDay(holiday).Should().Be(new LocalDate(2024, 6, 6));
    }

    [Fact]
    public void HolidayCalendar_ValueContractsHandleNullAndDescribeCalendar()
    {
        HolidayCalendar calendar = new([new LocalDate(2024, 6, 5)]);
        string? description = calendar.ToString();

        calendar.Equals(null).Should().BeFalse();
        description.Should().Be("HolidayCalendar(1 supplied holidays; weekends closed)");
    }

    [Fact]
    public void AprcConstructor_RejectsUndefinedPeriodUnit()
    {
        Action create = () => new AprcCalculator((AprcPeriodUnit)99);

        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(true, 100, "Principal and iteration limit must be positive.")]
    [InlineData(false, 0, "Principal and iteration limit must be positive.")]
    public void Aprc_ReportsInvalidPrincipalOrIterationLimit(
        bool zeroPrincipal,
        int iterations,
        string expectedMessage)
    {
        AprcCalculationResult result = new AprcCalculator().TryCalculate(
            new Money(zeroPrincipal ? 0m : 1_000m, Currency),
            [Payment(Start.PlusYears(1), 1_100m)],
            Start,
            iterations);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(expectedMessage);
    }

    [Fact]
    public void Aprc_RejectsMixedValidAndInvalidRepayments()
    {
        PaymentScheduleItem valid = Payment(Start.PlusMonths(6), 500m);
        PaymentScheduleItem invalidCurrency = new(
            2,
            Start.PlusYears(1),
            new Money(600m, "USD"),
            Money.Zero("USD"),
            new Money(600m, "USD"));

        AprcCalculationResult result = new AprcCalculator().TryCalculate(
            new Money(1_000m, Currency), [valid, invalidCurrency], Start);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(
            "Repayments must be non-negative, in the principal currency and on or after disbursement.");
    }

    [Fact]
    public void Aprc_AllowsZeroRepaymentWhenPositiveFutureRepaymentExists()
    {
        AprcCalculationResult result = new AprcCalculator().TryCalculate(
            new Money(1_000m, Currency),
            [Payment(Start.PlusMonths(1), 0m), Payment(Start.PlusYears(1), 1_100m, 2)],
            Start);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void Aprc_ReportsMissingPositiveFutureRepayment()
    {
        AprcCalculationResult result = new AprcCalculator().TryCalculate(
            new Money(1_000m, Currency), [Payment(Start, 100m)], Start);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("A positive repayment after disbursement is required.");
    }

    [Fact]
    public void Aprc_RequiresIsoDatesWithRepresentablePrecedingYear()
    {
        LocalDate copticStart = new(1740, 1, 1, CalendarSystem.Coptic);
        AprcCalculationResult nonIso = new AprcCalculator().TryCalculate(
            new Money(1_000m, Currency), [Payment(copticStart.PlusYears(1), 1_100m)], copticStart);
        LocalDate minimumIso = new(-9998, 1, 1);
        AprcCalculationResult minimumYear = new AprcCalculator().TryCalculate(
            new Money(1_000m, Currency), [Payment(minimumIso.PlusYears(1), 1_100m)], minimumIso);

        nonIso.Error.Should().Be("An ISO date with a representable preceding year is required.");
        minimumYear.Error.Should().Be("An ISO date with a representable preceding year is required.");
    }

    [Fact]
    public void AprcCalculate_ThrowsTheTryCalculateFailureReason()
    {
        Action calculate = () => new AprcCalculator().Calculate(
            new Money(1_000m, Currency), [], Start);

        calculate.Should().Throw<ArgumentException>()
            .WithMessage("*At least one repayment is required.*");
    }

    [Fact]
    public void Aprc_ShortResidualMonthUsesPublishedDayRule()
    {
        LocalDate start = new(2024, 1, 12);
        LocalDate paymentDate = new(2024, 2, 10);
        decimal expected = (decimal)(Math.Pow(1.1d, 365d / 29d) - 1d);

        Percentage result = new AprcCalculator().Calculate(
            new Money(1_000m, Currency), [Payment(paymentDate, 1_100m)], start);

        result.Fraction.Should().BeApproximately(expected, 0.00000001m);
    }

    private static AnnuityScheduleGenerator Generator() => new(new InterestCalculator());

    private static PaymentScheduleItem Payment(LocalDate date, decimal amount, int period = 1) =>
        new(period, date, new Money(amount, Currency), Money.Zero(Currency), new Money(amount, Currency));
}
