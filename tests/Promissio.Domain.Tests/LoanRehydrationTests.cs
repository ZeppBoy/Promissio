using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.Loan;
using Promissio.Domain.Loan.Events;
using Promissio.Domain.ValueObjects;
using Xunit;
using LoanRoot = Promissio.Domain.Loan.Loan;

namespace Promissio.Domain.Tests;

public sealed class LoanRehydrationTests
{
    private static readonly LoanId Id = new(Guid.Parse("ac08c3a2-4572-4cf1-aebc-825826ec9993"));
    private static readonly Guid ApplicationId = Guid.Parse("8c69fa59-70e4-4f70-ac88-ce56a1971f4e");
    private static readonly Guid CorrelationId = Guid.Parse("f633c93b-a82b-4ed1-88d4-c468631bef90");
    private static readonly LocalDate DisbursementDate = new(2026, 9, 10);
    private static readonly Instant RecordedAt = Instant.FromUtc(2026, 9, 10, 9, 30);
    private static readonly Money Principal = new(100_000m, "EUR");
    private static readonly FixedRate Rate = new(Percentage.FromPercent(5m), DayCountConventions.Actual365);
    private static readonly LoanTerm Term = LoanTerm.FromMonths(120);

    [Fact]
    public void Rehydrate_ReplaysLifecycleWithoutCreatingUncommittedEvents()
    {
        LoanRoot original = CreateLoan();
        original.Activate(
            DisbursementDate.PlusDays(1),
            new HolidayCalendar(Array.Empty<LocalDate>()),
            RecordedAt,
            CorrelationId);
        original.ApplyAging(95, DisbursementDate.PlusDays(95), RecordedAt, CorrelationId);
        original.WriteOff("Unrecoverable balance", DisbursementDate.PlusDays(96), RecordedAt, CorrelationId);
        LoanEvent[] history = original.UncommittedEvents.ToArray();

        LoanRoot rehydrated = LoanRoot.Rehydrate(history);

        rehydrated.Id.Should().Be(Id);
        rehydrated.LoanApplicationId.Should().Be(ApplicationId);
        rehydrated.TermsVersion.Should().Be(3);
        rehydrated.Principal.Should().Be(Principal);
        rehydrated.RemainingBalance.Should().Be(Principal);
        rehydrated.Rate.Should().Be(Rate);
        rehydrated.Term.Should().Be(Term);
        rehydrated.State.Should().Be(LoanState.WrittenOff);
        rehydrated.DaysPastDue.Should().Be(95);
        rehydrated.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_AppliesPersistedPaymentBalance()
    {
        LoanRoot original = CreateLoan();
        LoanCreated created = original.UncommittedEvents.OfType<LoanCreated>().Single();
        PaymentReceived payment = new(
            Id,
            DisbursementDate.PlusMonths(1),
            RecordedAt,
            CorrelationId,
            new Money(1_000m, "EUR"),
            new Money(99_000m, "EUR"));

        LoanRoot rehydrated = LoanRoot.Rehydrate([created, payment]);

        rehydrated.RemainingBalance.Should().Be(new Money(99_000m, "EUR"));
        rehydrated.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_EmptyStream_Throws()
    {
        Action action = () => LoanRoot.Rehydrate([]);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Rehydrate_StreamWithoutCreationEvent_Throws()
    {
        LoanActivated activated = new(
            Id,
            DisbursementDate.PlusDays(1),
            RecordedAt,
            CorrelationId,
            DisbursementDate);

        Action action = () => LoanRoot.Rehydrate([activated]);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*must begin with LoanCreated*");
    }

    [Fact]
    public void Rehydrate_EventForAnotherLoan_Throws()
    {
        LoanRoot original = CreateLoan();
        LoanCreated created = original.UncommittedEvents.OfType<LoanCreated>().Single();
        LoanActivated activated = new(
            LoanId.New(),
            DisbursementDate.PlusDays(1),
            RecordedAt,
            CorrelationId,
            DisbursementDate);

        Action action = () => LoanRoot.Rehydrate([created, activated]);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not match aggregate ID*");
    }

    private static LoanRoot CreateLoan() => new(
        Id,
        ApplicationId,
        3,
        Principal,
        Rate,
        Term,
        DisbursementDate,
        DisbursementDate.PlusMonths(1),
        RecordedAt,
        CorrelationId);
}
