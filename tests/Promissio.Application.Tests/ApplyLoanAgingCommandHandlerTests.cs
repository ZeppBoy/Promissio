using FluentAssertions;
using NodaTime;
using Promissio.Application.LoanActivation;
using Promissio.Application.LoanAging;
using Promissio.Application.LoanCreation;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.Loan;
using Promissio.Domain.Loan.Events;
using Promissio.Domain.ValueObjects;
using Xunit;
using LoanRoot = Promissio.Domain.Loan.Loan;

namespace Promissio.Application.Tests;

public sealed class ApplyLoanAgingCommandHandlerTests
{
    private static readonly LoanId Id = new(Guid.Parse("d7c6dc47-25cc-430e-bfa1-76bfe7c3e1bd"));
    private static readonly LocalDate EffectiveDate = new(2026, 9, 12);

    public static TheoryData<int, int, int, LoanState, Type> AgingTransitions => new()
    {
        { 1, 5, 90, LoanState.InGrace, typeof(LoanEnteredGracePeriod) },
        { 1, 1, 90, LoanState.PastDue, typeof(LoanBecamePastDue) },
        { 90, 1, 90, LoanState.Defaulted, typeof(LoanDefaulted) }
    };

    [Theory]
    [MemberData(nameof(AgingTransitions))]
    public async Task Handle_AppliesAndSavesTransitionAtLoadedVersion(
        int daysPastDue,
        int pastDueThreshold,
        int defaultThreshold,
        LoanState expectedState,
        Type expectedEventType)
    {
        PersistedLoan persistedLoan = CreateActivePersistedLoan();
        RecordingLoanRepository repository = new(persistedLoan, LoanSaveStatus.Saved);
        ApplyLoanAgingCommandHandler handler = new(repository);

        ApplyLoanAgingResult result = await handler.Handle(
            CreateCommand(daysPastDue, pastDueThreshold, defaultThreshold),
            CancellationToken.None);

        result.Should().Be(new ApplyLoanAgingResult(Id, ApplyLoanAgingStatus.Applied, expectedState));
        repository.SavedLoan.Should().BeSameAs(persistedLoan);
        repository.SavedLoan!.StreamVersion.Should().Be(2);
        repository.SavedLoan.Loan.DaysPastDue.Should().Be(daysPastDue);
        repository.SavedLoan.Loan.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType(expectedEventType);
    }

    [Fact]
    public async Task Handle_CuresPastDueLoanToActive()
    {
        PersistedLoan persistedLoan = CreatePastDuePersistedLoan();
        RecordingLoanRepository repository = new(persistedLoan, LoanSaveStatus.Saved);
        ApplyLoanAgingCommandHandler handler = new(repository);

        ApplyLoanAgingResult result = await handler.Handle(CreateCommand(0), CancellationToken.None);

        result.Should().Be(new ApplyLoanAgingResult(Id, ApplyLoanAgingStatus.Applied, LoanState.Active));
        repository.SavedLoan.Should().BeSameAs(persistedLoan);
        repository.SavedLoan!.Loan.DaysPastDue.Should().BeNull();
        repository.SavedLoan.Loan.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LoanBecameActive>();
    }

    [Fact]
    public async Task Handle_ActiveWithZeroDays_ReturnsNoChangeWithoutSaving()
    {
        RecordingLoanRepository repository = new(CreateActivePersistedLoan(), LoanSaveStatus.Saved);
        ApplyLoanAgingCommandHandler handler = new(repository);

        ApplyLoanAgingResult result = await handler.Handle(CreateCommand(0), CancellationToken.None);

        result.Should().Be(new ApplyLoanAgingResult(Id, ApplyLoanAgingStatus.NoChange, LoanState.Active));
        repository.SavedLoan.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MissingStream_ReturnsNotFoundWithoutSaving()
    {
        RecordingLoanRepository repository = new(null, LoanSaveStatus.Saved);
        ApplyLoanAgingCommandHandler handler = new(repository);

        ApplyLoanAgingResult result = await handler.Handle(CreateCommand(1), CancellationToken.None);

        result.Should().Be(new ApplyLoanAgingResult(Id, ApplyLoanAgingStatus.NotFound, null));
        repository.SavedLoan.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StaleStream_ReturnsConcurrencyConflictWithoutAuthoritativeState()
    {
        RecordingLoanRepository repository = new(
            CreateActivePersistedLoan(),
            LoanSaveStatus.ConcurrencyConflict);
        ApplyLoanAgingCommandHandler handler = new(repository);

        ApplyLoanAgingResult result = await handler.Handle(CreateCommand(1), CancellationToken.None);

        result.Should().Be(new ApplyLoanAgingResult(
            Id,
            ApplyLoanAgingStatus.ConcurrencyConflict,
            null));
    }

    [Fact]
    public async Task Handle_DisbursementState_DoesNotSaveInvalidTransition()
    {
        RecordingLoanRepository repository = new(CreateDisbursedPersistedLoan(), LoanSaveStatus.Saved);
        ApplyLoanAgingCommandHandler handler = new(repository);

        Func<Task> age = () => handler.Handle(CreateCommand(1), CancellationToken.None);

        await age.Should().ThrowAsync<InvalidStateTransitionException>();
        repository.SavedLoan.Should().BeNull();
    }

    private static ApplyLoanAgingCommand CreateCommand(
        int daysPastDue,
        int pastDueThreshold = 1,
        int defaultThreshold = 90) => new(
            Id,
            daysPastDue,
            EffectiveDate,
            Instant.FromUtc(2026, 9, 12, 2, 0),
            Guid.Parse("02113b31-6fd3-4f48-87ae-4557cd1c1e24"),
            pastDueThreshold,
            defaultThreshold);

    private static PersistedLoan CreateActivePersistedLoan()
    {
        PersistedLoan persistedLoan = CreateDisbursedPersistedLoan();
        persistedLoan.Loan.Activate(
            persistedLoan.Loan.DisbursementDate.PlusDays(1),
            new HolidayCalendar([]),
            Instant.FromUtc(2026, 9, 11, 9, 0),
            Guid.Parse("6bc5f9b6-9f75-41e2-ae95-f3c278d05df1"));
        persistedLoan.Loan.ClearUncommittedEvents();
        return persistedLoan with { StreamVersion = 2 };
    }

    private static PersistedLoan CreatePastDuePersistedLoan()
    {
        PersistedLoan persistedLoan = CreateActivePersistedLoan();
        persistedLoan.Loan.ApplyAging(
            15,
            EffectiveDate.Minus(Period.FromDays(1)),
            Instant.FromUtc(2026, 9, 11, 2, 0),
            Guid.Parse("ea56c0d4-8586-4222-b85c-320858bbeb77"));
        persistedLoan.Loan.ClearUncommittedEvents();
        return persistedLoan with { StreamVersion = 3 };
    }

    private static PersistedLoan CreateDisbursedPersistedLoan()
    {
        LocalDate disbursementDate = new(2026, 9, 10);
        LoanRoot loan = new(
            Id,
            Guid.Parse("0666025f-3bcd-40ec-ac59-733d6f251d8a"),
            1,
            new Money(25_000m, "EUR"),
            new FixedRate(Percentage.FromPercent(4.5m), DayCountConventions.Actual365),
            LoanTerm.FromMonths(60),
            disbursementDate,
            disbursementDate.PlusMonths(1),
            Instant.FromUtc(2026, 9, 10, 12, 0),
            Guid.Parse("d15985c1-f929-4441-9a25-5ba90e509754"));
        loan.ClearUncommittedEvents();
        return new PersistedLoan(loan, 1);
    }

    private sealed class RecordingLoanRepository(
        PersistedLoan? loadedLoan,
        LoanSaveStatus saveStatus) : ILoanRepository
    {
        public PersistedLoan? SavedLoan { get; private set; }

        public Task<LoanCreationResult> CreateAsync(
            LoanRoot loan,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PersistedLoan?> LoadAsync(
            LoanId loanId,
            CancellationToken cancellationToken) =>
            Task.FromResult(loadedLoan);

        public Task<LoanSaveStatus> SaveAsync(
            PersistedLoan persistedLoan,
            CancellationToken cancellationToken)
        {
            SavedLoan = persistedLoan;
            return Task.FromResult(saveStatus);
        }
    }
}
