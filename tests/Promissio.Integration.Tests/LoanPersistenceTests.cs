using FluentAssertions;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Promissio.Application.LoanActivation;
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
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
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
