using FluentAssertions;
using NodaTime;
using Promissio.Application.LoanCreation;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.Loan;
using Promissio.Domain.ValueObjects;
using Xunit;
using LoanRoot = Promissio.Domain.Loan.Loan;

namespace Promissio.Application.Tests;

public sealed class CreateLoanCommandHandlerTests
{
    private static readonly Guid ApplicationId = Guid.Parse("0c33d604-c161-446c-8792-a6f332571932");
    private static readonly Guid CorrelationId = Guid.Parse("5e73f4a3-aa2c-4b71-b60e-31de9e61a592");
    private static readonly LocalDate DisbursementDate = new(2026, 9, 10);
    private static readonly Instant RecordedAt = Instant.FromUtc(2026, 9, 10, 10, 0);

    [Fact]
    public async Task Handle_ConstructsDisbursedLoanFromApprovedTerms()
    {
        RecordingLoanRepository repository = new();
        CreateLoanCommandHandler handler = new(repository);
        CreateLoanCommand command = CreateCommand();

        LoanCreationResult result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(LoanCreationStatus.Created);
        repository.CreatedLoan.Should().NotBeNull();
        repository.CreatedLoan!.Id.Should().Be(result.LoanId);
        repository.CreatedLoan.LoanApplicationId.Should().Be(ApplicationId);
        repository.CreatedLoan.TermsVersion.Should().Be(2);
        repository.CreatedLoan.Principal.Should().Be(command.Principal);
        repository.CreatedLoan.Rate.Should().Be(command.Rate);
        repository.CreatedLoan.Term.Should().Be(command.Term);
        repository.CreatedLoan.State.Should().Be(LoanState.Disbursed);
        repository.CreatedLoan.UncommittedEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ReturnsRepositoryIdempotencyOutcome()
    {
        LoanId existingLoanId = new(Guid.Parse("da08ca57-13ec-475c-98a7-f66e720f8b5b"));
        RecordingLoanRepository repository = new(
            new LoanCreationResult(existingLoanId, LoanCreationStatus.Existing));
        CreateLoanCommandHandler handler = new(repository);

        LoanCreationResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Should().Be(new LoanCreationResult(existingLoanId, LoanCreationStatus.Existing));
    }

    private static CreateLoanCommand CreateCommand() => new(
        ApplicationId,
        2,
        new Money(25_000m, "EUR"),
        new FixedRate(Percentage.FromPercent(4.5m), DayCountConventions.Actual365),
        LoanTerm.FromMonths(60),
        DisbursementDate,
        DisbursementDate.PlusMonths(1),
        RecordedAt,
        CorrelationId);

    private sealed class RecordingLoanRepository : ILoanRepository
    {
        private readonly LoanCreationResult? _configuredResult;

        public RecordingLoanRepository(LoanCreationResult? configuredResult = null)
        {
            _configuredResult = configuredResult;
        }

        public LoanRoot? CreatedLoan { get; private set; }

        public Task<LoanCreationResult> CreateAsync(
            LoanRoot loan,
            CancellationToken cancellationToken)
        {
            CreatedLoan = loan;
            LoanCreationResult result = _configuredResult
                ?? new LoanCreationResult(loan.Id, LoanCreationStatus.Created);
            return Task.FromResult(result);
        }

        public Task<PersistedLoan?> LoadAsync(
            LoanId loanId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PersistedLoan?>(null);
    }
}
