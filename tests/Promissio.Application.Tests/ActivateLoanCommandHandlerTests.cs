using FluentAssertions;
using NodaTime;
using Promissio.Application.LoanActivation;
using Promissio.Application.LoanCreation;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.Loan;
using Promissio.Domain.Loan.Events;
using Promissio.Domain.ValueObjects;
using Xunit;
using LoanRoot = Promissio.Domain.Loan.Loan;

namespace Promissio.Application.Tests;

public sealed class ActivateLoanCommandHandlerTests
{
    private static readonly LoanId Id = new(Guid.Parse("38898af1-bf1e-4549-a599-da520393d547"));
    private static readonly LocalDate DisbursementDate = new(2026, 9, 10);
    private static readonly LocalDate ActivationDate = new(2026, 9, 11);

    [Fact]
    public async Task Handle_ActivatesAndSavesAtLoadedStreamVersion()
    {
        PersistedLoan persistedLoan = CreatePersistedLoan();
        RecordingLoanRepository repository = new(persistedLoan, LoanSaveStatus.Saved);
        ActivateLoanCommandHandler handler = new(repository);

        ActivateLoanResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Should().Be(new ActivateLoanResult(Id, ActivateLoanStatus.Activated));
        repository.SavedLoan.Should().BeSameAs(persistedLoan);
        repository.SavedLoan!.StreamVersion.Should().Be(1);
        repository.SavedLoan.Loan.State.Should().Be(LoanState.Active);
        repository.SavedLoan.Loan.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LoanActivated>();
    }

    [Fact]
    public async Task Handle_MissingStream_ReturnsNotFoundWithoutSaving()
    {
        RecordingLoanRepository repository = new(null, LoanSaveStatus.Saved);
        ActivateLoanCommandHandler handler = new(repository);

        ActivateLoanResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Should().Be(new ActivateLoanResult(Id, ActivateLoanStatus.NotFound));
        repository.SavedLoan.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StaleStream_ReturnsConcurrencyConflict()
    {
        RecordingLoanRepository repository = new(
            CreatePersistedLoan(),
            LoanSaveStatus.ConcurrencyConflict);
        ActivateLoanCommandHandler handler = new(repository);

        ActivateLoanResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Should().Be(new ActivateLoanResult(Id, ActivateLoanStatus.ConcurrencyConflict));
    }

    [Fact]
    public async Task Handle_InvalidActivationDate_DoesNotSave()
    {
        RecordingLoanRepository repository = new(CreatePersistedLoan(), LoanSaveStatus.Saved);
        ActivateLoanCommandHandler handler = new(repository);
        ActivateLoanCommand invalid = CreateCommand() with
        {
            EffectiveDate = DisbursementDate.PlusDays(2)
        };

        Func<Task> activate = () => handler.Handle(invalid, CancellationToken.None);

        await activate.Should().ThrowAsync<InvalidStateTransitionException>();
        repository.SavedLoan.Should().BeNull();
    }

    private static ActivateLoanCommand CreateCommand() => new(
        Id,
        ActivationDate,
        new HolidayCalendar([]),
        Instant.FromUtc(2026, 9, 11, 9, 0),
        Guid.Parse("cb6ddb3b-f460-4ba6-af95-59c23b218f74"));

    private static PersistedLoan CreatePersistedLoan()
    {
        LoanRoot loan = new(
            Id,
            Guid.Parse("5e20f88c-f2a6-4b27-9137-e3d5637231f1"),
            1,
            new Money(25_000m, "EUR"),
            new FixedRate(Percentage.FromPercent(4.5m), DayCountConventions.Actual365),
            LoanTerm.FromMonths(60),
            DisbursementDate,
            DisbursementDate.PlusMonths(1),
            Instant.FromUtc(2026, 9, 10, 12, 0),
            Guid.Parse("ec8a3ca6-ee92-4288-ab0b-98f5639d416f"));
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
