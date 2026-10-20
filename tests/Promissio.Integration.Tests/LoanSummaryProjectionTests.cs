using FluentAssertions;
using JasperFx.Events;
using Marten;
using Marten.Events;
using Marten.NodaTimePlugin;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Promissio.Application.LoanActivation;
using Promissio.Application.LoanAging;
using Promissio.Application.LoanCreation;
using Promissio.Application.LoanQueries;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.Loan;
using Promissio.Domain.Loan.Events;
using Promissio.Domain.ValueObjects;
using Promissio.Infrastructure.LoanPersistence;
using Promissio.Infrastructure.LoanQueries;
using Xunit;
using LoanRoot = Promissio.Domain.Loan.Loan;

namespace Promissio.Integration.Tests;

public sealed class LoanSummaryProjectionTests(PostgreSqlLoanFixture fixture)
    : IClassFixture<PostgreSqlLoanFixture>
{
    private ILoanRepository Repository => fixture.Repository;
    private ILoanSummaryReader Reader => fixture.Services.GetRequiredService<ILoanSummaryReader>();

    [Fact]
    public async Task Creation_IsImmediatelyVisibleThroughMediatR_AndRetryDoesNotChangeSummary()
    {
        LoanRoot loan = CreateLoan();
        await Repository.CreateAsync(loan, CancellationToken.None);
        ISender sender = fixture.Services.GetRequiredService<ISender>();

        LoanSummary? summary = await sender.Send(new GetLoanSummaryByIdQuery(loan.Id));

        summary.Should().Be(new LoanSummary(loan.Id.Value, loan.LoanApplicationId, loan.TermsVersion,
            loan.Principal, loan.DisbursementDate, loan.FirstPaymentDate, LoanState.Disbursed, null, 1));
        LoanRoot retry = CreateLoan(loan.LoanApplicationId);
        LoanCreationResult result = await Repository.CreateAsync(retry, CancellationToken.None);
        result.Status.Should().Be(LoanCreationStatus.Existing);
        (await Reader.ReadAsync(loan.Id, CancellationToken.None)).Should().Be(summary);
        (await Reader.ReadAsync(retry.Id, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task MissingLoan_ReturnsNull()
    {
        (await Reader.ReadAsync(LoanId.New(), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task ActivationAndAging_UpdateSummaryInTheAppendTransaction()
    {
        LoanRoot loan = await CreateActiveLoan();
        LoanSummary active = await ReadRequired(loan.Id);
        active.State.Should().Be(LoanState.Active);
        active.StreamVersion.Should().Be(2);

        ApplyLoanAgingCommandHandler handler = new(Repository);
        (int Days, LoanState State, int? ExpectedDays)[] cases =
        [
            (1, LoanState.InGrace, 1),
            (5, LoanState.PastDue, 5),
            (8, LoanState.PastDue, 8),
            (0, LoanState.Active, null),
            (90, LoanState.Defaulted, 90)
        ];
        long version = 2;
        foreach ((int days, LoanState state, int? expectedDays) in cases)
        {
            await handler.Handle(new ApplyLoanAgingCommand(loan.Id, days,
                loan.DisbursementDate.PlusDays(2), Instant.FromUtc(2026, 9, 12, 2, 0),
                Guid.NewGuid(), PastDueThreshold: 5), CancellationToken.None);
            LoanSummary summary = await ReadRequired(loan.Id);
            summary.State.Should().Be(state);
            summary.DaysPastDue.Should().Be(expectedDays);
            summary.StreamVersion.Should().Be(++version);
            summary.OriginalPrincipal.Should().Be(loan.Principal);
        }
    }

    [Fact]
    public async Task StaleAgingWriter_RollsBackProjection_AndNoOpLeavesItUnchanged()
    {
        LoanRoot loan = await CreateActiveLoan();
        LoanSummary before = await ReadRequired(loan.Id);
        ApplyLoanAgingCommandHandler handler = new(Repository);
        ApplyLoanAgingResult noOp = await handler.Handle(new ApplyLoanAgingCommand(loan.Id, 0,
            loan.DisbursementDate.PlusDays(2), Instant.FromUtc(2026, 9, 12, 2, 0), Guid.NewGuid()), CancellationToken.None);
        noOp.Status.Should().Be(ApplyLoanAgingStatus.NoChange);
        (await ReadRequired(loan.Id)).Should().Be(before);

        PersistedLoan first = await LoadRequired(loan.Id);
        PersistedLoan stale = await LoadRequired(loan.Id);
        first.Loan.ApplyAging(1, loan.DisbursementDate.PlusDays(2), Instant.FromUtc(2026, 9, 12, 2, 0), Guid.NewGuid());
        stale.Loan.ApplyAging(90, loan.DisbursementDate.PlusDays(2), Instant.FromUtc(2026, 9, 12, 2, 1), Guid.NewGuid());
        (await Repository.SaveAsync(first, CancellationToken.None)).Should().Be(LoanSaveStatus.Saved);
        LoanSummary committed = await ReadRequired(loan.Id);

        (await Repository.SaveAsync(stale, CancellationToken.None)).Should().Be(LoanSaveStatus.ConcurrencyConflict);

        (await ReadRequired(loan.Id)).Should().Be(committed);
        committed.State.Should().Be(LoanState.PastDue);
        committed.DaysPastDue.Should().Be(1);
        committed.StreamVersion.Should().Be(3);
        (await LoadRequired(loan.Id)).StreamVersion.Should().Be(3);
    }

    [Theory]
    [InlineData(LoanState.WrittenOff)]
    [InlineData(LoanState.Restructured)]
    [InlineData(LoanState.Recovered)]
    public async Task TerminalEvents_ProjectTheExistingDomainState(LoanState terminalState)
    {
        LoanRoot loan = await CreateActiveLoan();
        PersistedLoan persisted = await LoadRequired(loan.Id);
        LocalDate date = loan.DisbursementDate.PlusDays(2);
        Instant recordedAt = Instant.FromUtc(2026, 9, 12, 2, 0);
        persisted.Loan.ApplyAging(90, date, recordedAt, Guid.NewGuid());
        switch (terminalState)
        {
            case LoanState.WrittenOff:
                persisted.Loan.WriteOff("Integration fixture", date, recordedAt, Guid.NewGuid());
                break;
            case LoanState.Restructured:
                persisted.Loan.Restructure("Integration fixture", date, recordedAt, Guid.NewGuid());
                break;
            case LoanState.Recovered:
                persisted.Loan.Recover("Integration fixture", date, recordedAt, Guid.NewGuid());
                break;
        }
        (await Repository.SaveAsync(persisted, CancellationToken.None)).Should().Be(LoanSaveStatus.Saved);

        LoanSummary summary = await ReadRequired(loan.Id);
        summary.State.Should().Be(terminalState);
        summary.DaysPastDue.Should().Be(90);
        summary.StreamVersion.Should().Be(4);
    }

    [Fact]
    public async Task Rebuild_BackfillsExistingStreams_AndPreservesEventsReceiptsAndInlineResults()
    {
        LoanRoot currentLoan = await CreateActiveLoan();
        PersistedLoan agedLoan = await LoadRequired(currentLoan.Id);
        agedLoan.Loan.ApplyAging(8, currentLoan.DisbursementDate.PlusDays(5),
            Instant.FromUtc(2026, 9, 15, 2, 0), Guid.NewGuid());
        await Repository.SaveAsync(agedLoan, CancellationToken.None);
        // A later append with an earlier effective date must still be the current view.
        PersistedLoan backdated = await LoadRequired(currentLoan.Id);
        backdated.Loan.ApplyAging(5, currentLoan.DisbursementDate.PlusDays(2),
            Instant.FromUtc(2026, 9, 16, 2, 0), Guid.NewGuid());
        await Repository.SaveAsync(backdated, CancellationToken.None);
        LoanSummary before = await ReadRequired(currentLoan.Id);
        before.DaysPastDue.Should().Be(5);
        before.StreamVersion.Should().Be(4);
        // Simulate the pre-projection writer against the same database and original event payloads.
        using DocumentStore legacyStore = DocumentStore.For(options =>
        {
            options.Connection(fixture.ConnectionString);
            options.DatabaseSchemaName = "promissio";
            options.Events.DatabaseSchemaName = "promissio";
            options.Events.AppendMode = EventAppendMode.Rich;
            options.Events.UseMandatoryStreamTypeDeclaration = true;
            options.UseSystemTextJsonForSerialization(configure: settings => settings.AllowOutOfOrderMetadataProperties = true);
            options.UseNodaTime();
        });
        MartenLoanRepository legacyRepository = new(legacyStore);
        LoanRoot legacyLoan = CreateLoan();
        await legacyRepository.CreateAsync(legacyLoan, CancellationToken.None);
        (await Reader.ReadAsync(legacyLoan.Id, CancellationToken.None)).Should().BeNull();

        LoanSummaryRebuilder rebuilder = fixture.Services.GetRequiredService<LoanSummaryRebuilder>();
        await rebuilder.RebuildAsync(CancellationToken.None);

        (await ReadRequired(currentLoan.Id)).Should().Be(before);
        LoanSummary backfilled = await ReadRequired(legacyLoan.Id);
        backfilled.State.Should().Be(LoanState.Disbursed);
        backfilled.StreamVersion.Should().Be(1);
        (await LoadRequired(currentLoan.Id)).StreamVersion.Should().Be(4);
        (await LoadRequired(legacyLoan.Id)).StreamVersion.Should().Be(1);
        LoanCreationResult retry = await Repository.CreateAsync(CreateLoan(legacyLoan.LoanApplicationId), CancellationToken.None);
        retry.Status.Should().Be(LoanCreationStatus.Existing);
        retry.LoanId.Should().Be(legacyLoan.Id);

        await rebuilder.RebuildAsync(CancellationToken.None);
        (await ReadRequired(currentLoan.Id)).Should().Be(before);
        (await ReadRequired(legacyLoan.Id)).Should().Be(backfilled);
        // Writes resume normally after maintenance.
        await Activate(legacyLoan);
        (await ReadRequired(legacyLoan.Id)).StreamVersion.Should().Be(2);
    }

    [Fact]
    public async Task ReservedPaymentEvent_AdvancesVersionAndPreservesOriginalPrincipal()
    {
        LoanRoot loan = await CreateActiveLoan();
        LoanSummary before = await ReadRequired(loan.Id);
        // Replay compatibility only: payment allocation is not exposed by this slice.
        IDocumentStore store = fixture.Services.GetRequiredService<IDocumentStore>();
        await using IDocumentSession session = store.LightweightSession();
        session.Events.Append(loan.Id.Value, 3, new PaymentReceived(loan.Id,
            loan.FirstPaymentDate, Instant.FromUtc(2026, 10, 10, 12, 0), Guid.NewGuid(),
            loan.Principal, new Money(0m, "EUR")));
        await session.SaveChangesAsync();

        (await ReadRequired(loan.Id)).Should().Be(before with { StreamVersion = 3 });
    }

    private async Task<LoanRoot> CreateActiveLoan()
    {
        LoanRoot loan = CreateLoan();
        await Repository.CreateAsync(loan, CancellationToken.None);
        await Activate(loan);
        return loan;
    }

    private async Task Activate(LoanRoot loan)
    {
        ActivateLoanCommandHandler handler = new(Repository);
        ActivateLoanResult result = await handler.Handle(new ActivateLoanCommand(loan.Id,
            loan.DisbursementDate.PlusDays(1), new HolidayCalendar([]),
            Instant.FromUtc(2026, 9, 11, 9, 0), Guid.NewGuid()), CancellationToken.None);
        result.Status.Should().Be(ActivateLoanStatus.Activated);
    }

    private async Task<LoanSummary> ReadRequired(LoanId id) =>
        await Reader.ReadAsync(id, CancellationToken.None) ?? throw new InvalidOperationException("Summary missing.");

    private async Task<PersistedLoan> LoadRequired(LoanId id) =>
        await Repository.LoadAsync(id, CancellationToken.None) ?? throw new InvalidOperationException("Loan missing.");

    private static LoanRoot CreateLoan(Guid? applicationId = null) => new(
        LoanId.New(), applicationId ?? Guid.NewGuid(), 1, new Money(50_000m, "EUR"),
        new FixedRate(Percentage.FromPercent(4.25m), DayCountConventions.Actual365), LoanTerm.FromMonths(84),
        new LocalDate(2026, 9, 10), new LocalDate(2026, 10, 10), Instant.FromUtc(2026, 9, 10, 12, 0), Guid.NewGuid());
}
