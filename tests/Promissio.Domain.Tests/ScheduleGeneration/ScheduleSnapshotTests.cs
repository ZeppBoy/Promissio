using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ScheduleGeneration;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.Tests.ScheduleGeneration;

public class ScheduleSnapshotTests
{
    private readonly LocalDate _startDate = new LocalDate(2024, 1, 1);
    private readonly IInterestCalculator _interestCalculator = new InterestCalculator();

    [Fact]
    public void AnnuitySchedule_CanonicalCase_MatchesSnapshot()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);
        var term = 12;

        var generator = new AnnuityScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate).ToList();

        // Act & Assert
        Verify(schedule);
    }

    [Fact]
    public void DifferentiatedSchedule_CanonicalCase_MatchesSnapshot()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);
        var term = 12;

        var generator = new DifferentiatedScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate).ToList();

        // Act & Assert
        Verify(schedule);
    }

    [Fact]
    public void BulletSchedule_CanonicalCase_MatchesSnapshot()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);
        var term = 12;

        var generator = new BulletScheduleGenerator(_interestCalculator);
        var schedule = generator.Generate(principal, rate, term, _startDate).ToList();

        // Act & Assert
        Verify(schedule);
    }

    [Fact]
    public void CustomSchedule_CanonicalCase_MatchesSnapshot()
    {
        // Arrange
        var principal = new Money(10000, "USD");
        var rate = new FixedRate(new Percentage(0.10m), DayCountConventions.ActualActual);
        var term = 5;
        var customFlows = new List<CustomScheduleGenerator.CustomCashFlow>
        {
            new(new Money(1000, "USD"), new Money(500, "USD")),
            new(new Money(2000, "USD"), new Money(500, "USD")),
            new(new Money(3000, "USD"), new Money(500, "USD")),
            new(new Money(3000, "USD"), new Money(500, "USD")),
            new(new Money(1000, "USD"), new Money(500, "USD"))
        };

        var generator = new CustomScheduleGenerator(customFlows);
        var schedule = generator.Generate(principal, rate, term, _startDate).ToList();

        // Act & Assert
        Verify(schedule);
    }
}
