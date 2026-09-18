using System.Text;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Promissio.Application.LoanCreation;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.Loan;
using Promissio.Domain.Loan.Events;
using Promissio.Domain.ValueObjects;
using Promissio.Infrastructure;
using Promissio.Infrastructure.LoanPersistence;
using Xunit;

namespace Promissio.Infrastructure.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public void ConfigureMarten_RegistersLoanRepository()
    {
        ServiceCollection services = new();
        InfrastructureService.ConfigureMarten(
            services,
            "Host=localhost;Database=promissio;Username=promissio;Password=promissio");

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<MartenLoanRepository>(provider.GetRequiredService<ILoanRepository>());
    }

    [Fact]
    public void ConfigureMarten_RoundTripsLoanCreatedWithNodaTimeAndPolymorphicRate()
    {
        ServiceCollection services = new();
        InfrastructureService.ConfigureMarten(
            services,
            "Host=localhost;Database=promissio;Username=promissio;Password=promissio");

        using ServiceProvider provider = services.BuildServiceProvider();
        IDocumentStore store = provider.GetRequiredService<IDocumentStore>();
        LoanCreated original = new(
            new LoanId(Guid.Parse("a7513b2e-0559-47ae-be18-4252a98189d6")),
            Guid.Parse("55b0f919-cb43-46c4-9323-64fc8fe1f530"),
            4,
            new LocalDate(2026, 9, 10),
            Instant.FromUtc(2026, 9, 10, 12, 0),
            Guid.Parse("9676a05c-a412-4b41-a1c5-aa1303c9513e"),
            new Money(75_000m, "EUR"),
            new FixedRate(Percentage.FromPercent(5m), DayCountConventions.Actual365),
            LoanTerm.FromMonths(96),
            new LocalDate(2026, 9, 10),
            new LocalDate(2026, 10, 10));

        ISerializer serializer = store.Options.Serializer();
        string json = serializer.ToJson(original);
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
        LoanCreated roundTripped = serializer.FromJson<LoanCreated>(stream);

        Assert.Equal(original.LoanId, roundTripped.LoanId);
        Assert.Equal(original.EffectiveDate, roundTripped.EffectiveDate);
        Assert.Equal(original.RecordedAt, roundTripped.RecordedAt);
        Assert.Equal(original.Principal, roundTripped.Principal);
        Assert.Equal(original.Rate, roundTripped.Rate);
        Assert.Equal(original.Term, roundTripped.Term);
    }

    [Fact]
    public async Task CreateAsync_WithoutCreationEvent_RejectsBeforeDatabaseAccess()
    {
        ServiceCollection services = new();
        InfrastructureService.ConfigureMarten(
            services,
            "Host=localhost;Database=promissio;Username=promissio;Password=promissio");

        using ServiceProvider provider = services.BuildServiceProvider();
        ILoanRepository repository = provider.GetRequiredService<ILoanRepository>();
        Promissio.Domain.Loan.Loan loan = new(
            LoanId.New(),
            Guid.NewGuid(),
            1,
            new Money(10_000m, "EUR"),
            new FixedRate(Percentage.FromPercent(5m), DayCountConventions.Actual365),
            LoanTerm.FromMonths(24),
            new LocalDate(2026, 9, 10),
            new LocalDate(2026, 10, 10),
            Instant.FromUtc(2026, 9, 10, 12, 0),
            Guid.NewGuid());
        loan.ClearUncommittedEvents();

        Task Action() => repository.CreateAsync(loan, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(Action);
    }
}
