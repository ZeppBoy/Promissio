using FluentAssertions;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Promissio.Application.LoanActivation;
using Promissio.Application.LoanAging;
using Promissio.Application.LoanCreation;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.Loan;
using Promissio.Domain.ValueObjects;
using Promissio.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;
using LoanRoot = Promissio.Domain.Loan.Loan;

namespace Promissio.Integration.Tests;

public sealed class PostgreSqlLoanFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();
    private ServiceProvider? _serviceProvider;
    private ILoanRepository? _repository;

    public ILoanRepository Repository =>
        _repository ?? throw new InvalidOperationException("The test fixture has not been initialized.");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        ServiceCollection services = new();
        InfrastructureService.ConfigureMarten(services, _postgres.GetConnectionString());
        _serviceProvider = services.BuildServiceProvider();

        IDocumentStore store = _serviceProvider.GetRequiredService<IDocumentStore>();
        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        _repository = _serviceProvider.GetRequiredService<ILoanRepository>();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
            await _serviceProvider.DisposeAsync();

        await _postgres.DisposeAsync();
    }
}

public sealed class LoanPersistenceTests(PostgreSqlLoanFixture fixture)
    : IClassFixture<PostgreSqlLoanFixture>
{
    private ILoanRepository Repository => fixture.Repository;

    [Fact]
    public async Task CreateAndLoad_RoundTripsLoanThroughEventStream()
    {
        LoanRoot loan = CreateLoan(Guid.NewGuid());

        LoanCreationResult result = await Repository.CreateAsync(loan, CancellationToken.None);
        PersistedLoan? persisted = await Repository.LoadAsync(result.LoanId, CancellationToken.None);

        result.Status.Should().Be(LoanCreationStatus.Created);
        loan.UncommittedEvents.Should().BeEmpty();
        persisted.Should().NotBeNull();
        PersistedLoan loaded = persisted
            ?? throw new InvalidOperationException("The persisted loan was not returned.");
        loaded.StreamVersion.Should().Be(1);
        loaded.Loan.Id.Should().Be(loan.Id);
        loaded.Loan.LoanApplicationId.Should().Be(loan.LoanApplicationId);
        loaded.Loan.Principal.Should().Be(loan.Principal);
        loaded.Loan.Rate.Should().Be(loan.Rate);
        loaded.Loan.Term.Should().Be(loan.Term);
        loaded.Loan.State.Should().Be(LoanState.Disbursed);
        loaded.Loan.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task IdenticalRetry_ReturnsExistingLoanWithoutSecondStream()
    {
        Guid applicationId = Guid.NewGuid();
        LoanRoot firstAttempt = CreateLoan(applicationId);
        LoanRoot retry = CreateLoan(applicationId);

        LoanCreationResult first = await Repository.CreateAsync(firstAttempt, CancellationToken.None);
        LoanCreationResult second = await Repository.CreateAsync(retry, CancellationToken.None);
        PersistedLoan? retryStream = await Repository.LoadAsync(retry.Id, CancellationToken.None);

        first.Status.Should().Be(LoanCreationStatus.Created);
        second.Should().Be(new LoanCreationResult(first.LoanId, LoanCreationStatus.Existing));
        retryStream.Should().BeNull();
    }

    [Fact]
    public async Task ChangedTermsRetry_ReturnsConflictWithoutSecondStream()
    {
        Guid applicationId = Guid.NewGuid();
        LoanRoot firstAttempt = CreateLoan(applicationId);
        LoanRoot changedRetry = CreateLoan(
            applicationId,
            termsVersion: 2,
            principal: new Money(60_000m, "EUR"));

        LoanCreationResult first = await Repository.CreateAsync(firstAttempt, CancellationToken.None);
        LoanCreationResult second = await Repository.CreateAsync(changedRetry, CancellationToken.None);
        PersistedLoan? retryStream = await Repository.LoadAsync(changedRetry.Id, CancellationToken.None);

        first.Status.Should().Be(LoanCreationStatus.Created);
        second.Should().Be(new LoanCreationResult(first.LoanId, LoanCreationStatus.Conflict));
        retryStream.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentIdenticalHandoffs_CreateExactlyOneStream()
    {
        Guid applicationId = Guid.NewGuid();
        LoanRoot firstAttempt = CreateLoan(applicationId);
        LoanRoot secondAttempt = CreateLoan(applicationId);

        LoanCreationResult[] results = await Task.WhenAll(
            Repository.CreateAsync(firstAttempt, CancellationToken.None),
            Repository.CreateAsync(secondAttempt, CancellationToken.None));

        results.Should().ContainSingle(result => result.Status == LoanCreationStatus.Created);
        results.Should().ContainSingle(result => result.Status == LoanCreationStatus.Existing);
        results.Select(result => result.LoanId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task ActivateWorkflow_AppendsAndReplaysAtExpectedVersion()
    {
        LoanRoot loan = CreateLoan(Guid.NewGuid());
        LoanCreationResult created = await Repository.CreateAsync(loan, CancellationToken.None);
        ActivateLoanCommandHandler handler = new(Repository);
        ActivateLoanResult activation = await handler.Handle(new ActivateLoanCommand(
            created.LoanId,
            loan.DisbursementDate.PlusDays(1),
            new HolidayCalendar([]),
            Instant.FromUtc(2026, 9, 11, 9, 0),
            Guid.NewGuid()), CancellationToken.None);
        PersistedLoan reloaded = await LoadRequired(created.LoanId);

        activation.Should().Be(new ActivateLoanResult(created.LoanId, ActivateLoanStatus.Activated));
        reloaded.StreamVersion.Should().Be(2);
        reloaded.Loan.State.Should().Be(LoanState.Active);
    }

    [Fact]
    public async Task Activate_WithStaleVersion_ReturnsConflictWithoutThirdEvent()
    {
        LoanRoot loan = CreateLoan(Guid.NewGuid());
        LoanCreationResult created = await Repository.CreateAsync(loan, CancellationToken.None);
        PersistedLoan firstWriter = await LoadRequired(created.LoanId);
        PersistedLoan staleWriter = await LoadRequired(created.LoanId);
        LocalDate activationDate = firstWriter.Loan.DisbursementDate.PlusDays(1);
        firstWriter.Loan.Activate(
            activationDate,
            new HolidayCalendar([]),
            Instant.FromUtc(2026, 9, 11, 9, 0),
            Guid.NewGuid());
        staleWriter.Loan.Activate(
            activationDate,
            new HolidayCalendar([]),
            Instant.FromUtc(2026, 9, 11, 9, 1),
            Guid.NewGuid());

        LoanSaveStatus first = await Repository.SaveAsync(firstWriter, CancellationToken.None);
        LoanSaveStatus stale = await Repository.SaveAsync(staleWriter, CancellationToken.None);
        PersistedLoan reloaded = await LoadRequired(created.LoanId);

        first.Should().Be(LoanSaveStatus.Saved);
        stale.Should().Be(LoanSaveStatus.ConcurrencyConflict);
        reloaded.StreamVersion.Should().Be(2);
        reloaded.Loan.State.Should().Be(LoanState.Active);
    }

    [Fact]
    public async Task AgingWorkflow_AppendsAndReplaysGracePastDueCureAndDefaultTransitions()
    {
        LoanRoot loan = CreateLoan(Guid.NewGuid());
        LoanCreationResult created = await Repository.CreateAsync(loan, CancellationToken.None);
        await Activate(created.LoanId, loan.DisbursementDate.PlusDays(1));
        ApplyLoanAgingCommandHandler handler = new(Repository);

        ApplyLoanAgingResult grace = await handler.Handle(
            CreateAgingCommand(created.LoanId, 1, pastDueThreshold: 5),
            CancellationToken.None);
        PersistedLoan afterGrace = await LoadRequired(created.LoanId);
        ApplyLoanAgingResult pastDue = await handler.Handle(
            CreateAgingCommand(created.LoanId, 5, pastDueThreshold: 5),
            CancellationToken.None);
        PersistedLoan afterPastDue = await LoadRequired(created.LoanId);
        ApplyLoanAgingResult cured = await handler.Handle(
            CreateAgingCommand(created.LoanId, 0),
            CancellationToken.None);
        PersistedLoan afterCure = await LoadRequired(created.LoanId);
        ApplyLoanAgingResult defaulted = await handler.Handle(
            CreateAgingCommand(created.LoanId, 90),
            CancellationToken.None);
        PersistedLoan afterDefault = await LoadRequired(created.LoanId);

        grace.Should().Be(new ApplyLoanAgingResult(
            created.LoanId,
            ApplyLoanAgingStatus.Applied,
            LoanState.InGrace));
        afterGrace.StreamVersion.Should().Be(3);
        afterGrace.Loan.State.Should().Be(LoanState.InGrace);
        afterGrace.Loan.DaysPastDue.Should().Be(1);
        pastDue.Should().Be(new ApplyLoanAgingResult(
            created.LoanId,
            ApplyLoanAgingStatus.Applied,
            LoanState.PastDue));
        afterPastDue.StreamVersion.Should().Be(4);
        afterPastDue.Loan.State.Should().Be(LoanState.PastDue);
        afterPastDue.Loan.DaysPastDue.Should().Be(5);
        cured.Should().Be(new ApplyLoanAgingResult(
            created.LoanId,
            ApplyLoanAgingStatus.Applied,
            LoanState.Active));
        afterCure.StreamVersion.Should().Be(5);
        afterCure.Loan.State.Should().Be(LoanState.Active);
        afterCure.Loan.DaysPastDue.Should().BeNull();
        defaulted.Should().Be(new ApplyLoanAgingResult(
            created.LoanId,
            ApplyLoanAgingStatus.Applied,
            LoanState.Defaulted));
        afterDefault.StreamVersion.Should().Be(6);
        afterDefault.Loan.State.Should().Be(LoanState.Defaulted);
        afterDefault.Loan.DaysPastDue.Should().Be(90);
    }

    [Fact]
    public async Task AgingWorkflow_ActiveWithZeroDaysDoesNotAppendEvent()
    {
        LoanRoot loan = CreateLoan(Guid.NewGuid());
        LoanCreationResult created = await Repository.CreateAsync(loan, CancellationToken.None);
        await Activate(created.LoanId, loan.DisbursementDate.PlusDays(1));
        ApplyLoanAgingCommandHandler handler = new(Repository);

        ApplyLoanAgingResult result = await handler.Handle(
            CreateAgingCommand(created.LoanId, 0),
            CancellationToken.None);
        PersistedLoan reloaded = await LoadRequired(created.LoanId);

        result.Should().Be(new ApplyLoanAgingResult(
            created.LoanId,
            ApplyLoanAgingStatus.NoChange,
            LoanState.Active));
        reloaded.StreamVersion.Should().Be(2);
        reloaded.Loan.State.Should().Be(LoanState.Active);
    }

    [Fact]
    public async Task Aging_WithStaleVersion_ReturnsConflictWithoutSecondAgingEvent()
    {
        LoanRoot loan = CreateLoan(Guid.NewGuid());
        LoanCreationResult created = await Repository.CreateAsync(loan, CancellationToken.None);
        await Activate(created.LoanId, loan.DisbursementDate.PlusDays(1));
        PersistedLoan firstWriter = await LoadRequired(created.LoanId);
        PersistedLoan staleWriter = await LoadRequired(created.LoanId);
        firstWriter.Loan.ApplyAging(
            1,
            new LocalDate(2026, 9, 12),
            Instant.FromUtc(2026, 9, 12, 2, 0),
            Guid.NewGuid());
        staleWriter.Loan.ApplyAging(
            2,
            new LocalDate(2026, 9, 12),
            Instant.FromUtc(2026, 9, 12, 2, 1),
            Guid.NewGuid());

        LoanSaveStatus first = await Repository.SaveAsync(firstWriter, CancellationToken.None);
        LoanSaveStatus stale = await Repository.SaveAsync(staleWriter, CancellationToken.None);
        PersistedLoan reloaded = await LoadRequired(created.LoanId);

        first.Should().Be(LoanSaveStatus.Saved);
        stale.Should().Be(LoanSaveStatus.ConcurrencyConflict);
        reloaded.StreamVersion.Should().Be(3);
        reloaded.Loan.State.Should().Be(LoanState.PastDue);
        reloaded.Loan.DaysPastDue.Should().Be(1);
    }

    private async Task Activate(LoanId loanId, LocalDate activationDate)
    {
        ActivateLoanCommandHandler handler = new(Repository);
        ActivateLoanResult result = await handler.Handle(new ActivateLoanCommand(
            loanId,
            activationDate,
            new HolidayCalendar([]),
            Instant.FromUtc(2026, 9, 11, 9, 0),
            Guid.NewGuid()), CancellationToken.None);
        result.Status.Should().Be(ActivateLoanStatus.Activated);
    }

    private static ApplyLoanAgingCommand CreateAgingCommand(
        LoanId loanId,
        int daysPastDue,
        int pastDueThreshold = 1) => new(
            loanId,
            daysPastDue,
            new LocalDate(2026, 9, 12),
            Instant.FromUtc(2026, 9, 12, 2, 0),
            Guid.NewGuid(),
            pastDueThreshold,
            90);

    private async Task<PersistedLoan> LoadRequired(LoanId loanId) =>
        await Repository.LoadAsync(loanId, CancellationToken.None)
        ?? throw new InvalidOperationException($"Loan {loanId} was not found.");

    private static LoanRoot CreateLoan(
        Guid applicationId,
        int termsVersion = 1,
        Money? principal = null)
    {
        LocalDate disbursementDate = new(2026, 9, 10);
        return new LoanRoot(
            LoanId.New(),
            applicationId,
            termsVersion,
            principal ?? new Money(50_000m, "EUR"),
            new FixedRate(Percentage.FromPercent(4.25m), DayCountConventions.Actual365),
            LoanTerm.FromMonths(84),
            disbursementDate,
            disbursementDate.PlusMonths(1),
            Instant.FromUtc(2026, 9, 10, 12, 0),
            Guid.NewGuid());
    }
}
